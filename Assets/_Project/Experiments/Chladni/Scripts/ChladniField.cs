using Unity.Burst;
using Unity.Mathematics;

namespace PhysicsLab.Experiments.Chladni
{
    [BurstCompile]
    public static class ChladniField
    {
        // Chladni-style mode shape on a unit square in [0,1]^2:
        //   u(x,y) = cos(n π x) cos(m π y) − cos(m π x) cos(n π y)
        // Particles drift along −∇(u²) toward the nodal set {u = 0}.

        public static float Amplitude(float x, float y, float n, float m)
        {
            float nx = n * math.PI * x;
            float ny = n * math.PI * y;
            float mx = m * math.PI * x;
            float my = m * math.PI * y;
            return math.cos(nx) * math.cos(my) - math.cos(mx) * math.cos(ny);
        }

        public static float2 Gradient(float x, float y, float n, float m)
        {
            float nx = n * math.PI * x;
            float ny = n * math.PI * y;
            float mx = m * math.PI * x;
            float my = m * math.PI * y;

            float dudx = -n * math.PI * math.sin(nx) * math.cos(my)
                         + m * math.PI * math.sin(mx) * math.cos(ny);
            float dudy = -m * math.PI * math.cos(nx) * math.sin(my)
                         + n * math.PI * math.cos(mx) * math.sin(ny);
            return new float2(dudx, dudy);
        }

        public static void Sample(float x, float y, float n, float m, out float u, out float2 grad)
        {
            float nx = n * math.PI * x;
            float ny = n * math.PI * y;
            float mx = m * math.PI * x;
            float my = m * math.PI * y;

            float cnx = math.cos(nx), cmy = math.cos(my);
            float cmx = math.cos(mx), cny = math.cos(ny);
            float snx = math.sin(nx), smy = math.sin(my);
            float smx = math.sin(mx), sny = math.sin(ny);

            u = cnx * cmy - cmx * cny;
            float dudx = -n * math.PI * snx * cmy + m * math.PI * smx * cny;
            float dudy = -m * math.PI * cnx * smy + n * math.PI * cmx * sny;
            grad = new float2(dudx, dudy);
        }
    }
}
