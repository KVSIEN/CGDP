using UnityEngine;

namespace CGD.Level
{
    // The vault over an open-air room (a park, a town): a dome whose rim is exactly a rectangle
    // and whose crown is `height` above it, seen from inside. Rings of vertices run from the rim
    // up toward the crown, each ring a scaled copy of the rim, so the dome meets the room's top
    // edge all round instead of overhanging it like a round dome over a long room would.
    //
    // Plain geometry (arrays, no Mesh) so it can be checked without a scene. Vertices are
    // relative to the middle of the rim: x and z across the room, y up from the rim.
    public sealed class SkyDomeShape
    {
        public const int MinSegments = 8;
        public const int MinRings    = 2;

        // `size`: the rim's width (x) and depth (z). `rings`: rings of vertices below the crown.
        public SkyDomeShape(Vector2 size, float height, int segments, int rings)
        {
            segments = Mathf.Max(MinSegments, segments);
            rings    = Mathf.Max(MinRings, rings);
            Segments = segments;
            Rings    = rings;

            int count = segments * rings + 1;
            Vertices  = new Vector3[count];
            Elevation = new float[count];
            for (int k = 0; k < rings; k++)
            {
                float lift = (float)k / rings * Mathf.PI * 0.5f;
                for (int j = 0; j < segments; j++)
                {
                    float around = j * Mathf.PI * 2f / segments;
                    float reach  = RimReach(size, around) * Mathf.Cos(lift);
                    int i = k * segments + j;
                    Vertices[i]  = new Vector3(Mathf.Cos(around) * reach, height * Mathf.Sin(lift), Mathf.Sin(around) * reach);
                    Elevation[i] = (float)k / rings;
                }
            }
            Crown = count - 1;
            Vertices[Crown]  = new Vector3(0f, height, 0f);
            Elevation[Crown] = 1f;

            Triangles = BuildTriangles(segments, rings);
        }

        public int Segments { get; }
        public int Rings    { get; }
        public Vector3[] Vertices  { get; }
        // 0 at the rim to 1 at the crown, for each vertex: what the sky's colour is read from.
        public float[] Elevation   { get; }
        // Wound to face the inside of the dome.
        public int[] Triangles     { get; }
        public int Crown           { get; }

        // How far the rim is from its middle in the direction `around` (radians from +x toward +z).
        private static float RimReach(Vector2 size, float around)
        {
            float across = Mathf.Abs(Mathf.Cos(around)), along = Mathf.Abs(Mathf.Sin(around));
            float x = across > 1e-4f ? size.x * 0.5f / across : float.MaxValue;
            float z = along  > 1e-4f ? size.y * 0.5f / along  : float.MaxValue;
            return Mathf.Min(x, z);
        }

        private static int[] BuildTriangles(int segments, int rings)
        {
            var triangles = new int[((rings - 1) * 2 * segments + segments) * 3];
            int t = 0;
            for (int k = 0; k < rings - 1; k++)
                for (int j = 0; j < segments; j++)
                {
                    int a = k * segments + j, b = k * segments + (j + 1) % segments;
                    int c = a + segments,     d = b + segments;
                    triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                    triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                }

            int crown = segments * rings, top = (rings - 1) * segments;
            for (int j = 0; j < segments; j++)
            {
                triangles[t++] = top + j;
                triangles[t++] = top + (j + 1) % segments;
                triangles[t++] = crown;
            }
            return triangles;
        }
    }
}
