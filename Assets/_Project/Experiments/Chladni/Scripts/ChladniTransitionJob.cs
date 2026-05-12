using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PhysicsLab.Experiments.Chladni
{
    // Lerps each grain from its snapshot position toward its target with a
    // smoothstep curve, while still writing the per-instance matrix. Used by
    // the simulator during a reset so grains glide to fresh random positions
    // instead of teleporting (which read as a flash with thousands of grains).
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    public struct ChladniTransitionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> Src;
        [ReadOnly] public NativeArray<float2> Dst;

        [WriteOnly] public NativeArray<float2> Positions;
        [WriteOnly] public NativeArray<Matrix4x4> Matrices;

        public float Alpha;             // 0..1 linear progress
        public float3 PlateOrigin;
        public float PlateSize;
        public float GrainScale;
        public float GrainHeight;

        public void Execute(int index)
        {
            float a = math.saturate(Alpha);
            float k = a * a * (3f - 2f * a);
            float2 p = math.lerp(Src[index], Dst[index], k);
            Positions[index] = p;

            float3 world = PlateOrigin + new float3(p.x * PlateSize, GrainHeight, p.y * PlateSize);
            Matrix4x4 m = default;
            m.m00 = GrainScale; m.m11 = GrainScale; m.m22 = GrainScale; m.m33 = 1f;
            m.m03 = world.x; m.m13 = world.y; m.m23 = world.z;
            Matrices[index] = m;
        }
    }
}
