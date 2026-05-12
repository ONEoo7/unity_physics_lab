using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PhysicsLab.Experiments.Chladni
{
    public sealed class ChladniSaltSimulator : MonoBehaviour
    {
        [Header("Plate")]
        [SerializeField] private Transform plate;
        [SerializeField] private float plateSize = 1.0f;
        [SerializeField] private float grainLift = 0.005f;

        [Header("Grains")]
        [SerializeField] private Mesh grainMesh;
        [SerializeField] private Material grainMaterial;
        [SerializeField, Min(64)] private int desktopGrainCount = 20000;
        [SerializeField, Min(64)] private int mobileGrainCount = 5000;
        [SerializeField] private float grainScale = 0.008f;

        [Header("Tuning")]
        [SerializeField] private float driftStrength = 0.4f;
        [SerializeField] private float jitterStrength = 0.12f;
        [SerializeField, Range(0f, 1f)] private float settleDamping = 0.05f;
        [SerializeField] private float maxStepPerFrame = 0.02f;

        private NativeArray<float2> positions;
        private NativeArray<uint> randomStates;
        private NativeArray<Matrix4x4> matrices;
        private Matrix4x4[] renderBatch;
        private JobHandle handle;
        private bool initialized;

        public int GrainCount => positions.IsCreated ? positions.Length : 0;

        // Driven by ChladniController each frame.
        public float NA = 1f, MA = 2f, NB = 2f, MB = 3f, Blend = 0f;
        public float VibrationEnvelope = 1f;

        private void Start()
        {
            int count = PickGrainCount();
            positions = new NativeArray<float2>(count, Allocator.Persistent);
            randomStates = new NativeArray<uint>(count, Allocator.Persistent);
            matrices = new NativeArray<Matrix4x4>(count, Allocator.Persistent);
            renderBatch = new Matrix4x4[1023];

            var rng = new System.Random(12345);
            for (int i = 0; i < count; i++)
            {
                positions[i] = new float2((float)rng.NextDouble(), (float)rng.NextDouble());
                randomStates[i] = (uint)(rng.Next(1, int.MaxValue));
            }
            initialized = true;
        }

        public void ResetGrains()
        {
            if (!initialized) return;
            handle.Complete();
            var rng = new System.Random(Environment.TickCount);
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new float2((float)rng.NextDouble(), (float)rng.NextDouble());
                randomStates[i] = (uint)(rng.Next(1, int.MaxValue));
            }
        }

        private int PickGrainCount()
        {
#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
            return mobileGrainCount;
#else
            return SystemInfo.graphicsMemorySize < 2048 ? mobileGrainCount : desktopGrainCount;
#endif
        }

        private void Update()
        {
            if (!initialized) return;

            var origin = (plate != null ? plate.position : transform.position)
                         - new Vector3(plateSize * 0.5f, 0f, plateSize * 0.5f);

            var job = new ChladniSaltJob
            {
                Positions = positions,
                RandomStates = randomStates,
                Matrices = matrices,
                NA = NA, MA = MA, NB = NB, MB = MB, Blend = Blend,
                VibrationEnvelope = VibrationEnvelope,
                DriftStrength = driftStrength,
                JitterStrength = jitterStrength,
                SettleDamping = settleDamping,
                MaxStepPerFrame = maxStepPerFrame,
                DeltaTime = Time.deltaTime,
                PlateOrigin = origin,
                PlateSize = plateSize,
                GrainScale = grainScale,
                GrainHeight = grainLift,
            };
            handle = job.Schedule(positions.Length, 256);
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            handle.Complete();

            if (grainMesh == null || grainMaterial == null) return;

            int total = matrices.Length;
            int idx = 0;
            while (idx < total)
            {
                int batch = Mathf.Min(1023, total - idx);
                NativeArray<Matrix4x4>.Copy(matrices, idx, renderBatch, 0, batch);
                Graphics.DrawMeshInstanced(grainMesh, 0, grainMaterial, renderBatch, batch);
                idx += batch;
            }
        }

        private void OnDestroy()
        {
            handle.Complete();
            if (positions.IsCreated) positions.Dispose();
            if (randomStates.IsCreated) randomStates.Dispose();
            if (matrices.IsCreated) matrices.Dispose();
        }
    }
}
