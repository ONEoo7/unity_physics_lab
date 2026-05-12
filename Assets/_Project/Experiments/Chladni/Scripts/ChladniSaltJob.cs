using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PhysicsLab.Experiments.Chladni
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast)]
    public struct ChladniSaltJob : IJobParallelFor
    {
        public NativeArray<float2> Positions;     // grain XY in [0,1]
        public NativeArray<uint> RandomStates;    // per-grain RNG state

        [WriteOnly] public NativeArray<Matrix4x4> Matrices;

        // Two blended modes.
        public float NA, MA, NB, MB, Blend;

        // Vibration envelope: amplitude of plate motion this frame.
        public float VibrationEnvelope;

        // Tuning.
        public float DriftStrength;     // how strongly grains flow down |u|^2 gradient
        public float JitterStrength;    // bouncing intensity, scaled by local |u|
        public float SettleDamping;     // 0..1, fraction of jitter that survives at zero amplitude
        public float DeltaTime;

        // World placement of the plate.
        public float3 PlateOrigin;      // world position of plate corner (x=0, y=0)
        public float PlateSize;         // plate side length (world units)
        public float GrainScale;        // visual size (world units)
        public float GrainHeight;       // small lift above plate so grains aren't z-fighting

        public void Execute(int index)
        {
            float2 p = Positions[index];

            ChladniField.Sample(p.x, p.y, NA, MA, out float uA, out float2 gA);
            ChladniField.Sample(p.x, p.y, NB, MB, out float uB, out float2 gB);
            float u = math.lerp(uA, uB, Blend);
            float2 grad = math.lerp(gA, gB, Blend);

            // Drift along -∇(u²) = -2 u ∇u
            float2 drift = -2f * u * grad * DriftStrength;

            // Bouncing jitter — survives a little even at u=0 so settled grains still wiggle subtly.
            float jitterScale = (math.abs(u) * (1f - SettleDamping) + SettleDamping)
                                * JitterStrength * VibrationEnvelope;
            uint state = RandomStates[index];
            float2 rnd = new float2(NextSymmetric(ref state), NextSymmetric(ref state));
            RandomStates[index] = state;

            p += (drift + rnd * jitterScale) * DeltaTime;

            // Reflect off plate edges.
            if (p.x < 0f) { p.x = -p.x; }
            else if (p.x > 1f) { p.x = 2f - p.x; }
            if (p.y < 0f) { p.y = -p.y; }
            else if (p.y > 1f) { p.y = 2f - p.y; }
            p = math.clamp(p, 0f, 1f);

            Positions[index] = p;

            float3 world = PlateOrigin + new float3(p.x * PlateSize, GrainHeight, p.y * PlateSize);

            // TRS with identity rotation, uniform scale. Build directly; cheaper
            // and friendlier for Burst than calling Matrix4x4.TRS.
            Matrix4x4 m = default;
            m.m00 = GrainScale; m.m11 = GrainScale; m.m22 = GrainScale; m.m33 = 1f;
            m.m03 = world.x; m.m13 = world.y; m.m23 = world.z;
            Matrices[index] = m;
        }

        // xorshift32, returns value in (-1, 1).
        private static float NextSymmetric(ref uint state)
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x == 0u ? 1u : x;
            return ((state & 0x00FFFFFFu) / (float)0x00FFFFFFu) * 2f - 1f;
        }
    }
}
