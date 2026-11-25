using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CGD.Level
{
    // Accumulates rooms' overgrowth into one mesh: each overgrown tile a grid of quads at the
    // field's samples, lying on that tile's floor. Neighbouring tiles share their edge samples,
    // and their normals come from the field itself, so the surface runs on across tiles without
    // a seam. Quads with no growth at any corner are left out: the floor shows there. UVs are in
    // metres, as BoxMeshBuilder's are.
    public sealed class OvergrowthMeshBuilder
    {
        // Lifts the growth off the floor it lies on, so where it thins to nothing the two never flicker.
        private const float Lift = 0.02f;

        private readonly List<Vector3> _vertices  = new();
        private readonly List<Vector3> _normals   = new();
        private readonly List<Vector2> _uvs       = new();
        private readonly List<int>     _triangles = new();

        public bool IsEmpty => _triangles.Count == 0;

        public void AddRoom(LevelLayout layout, LevelRoom room)
        {
            OvergrowthField field = room.Overgrowth;
            if (field == null) return;

            foreach (Vector2Int tile in room.Footprint.Tiles)
                if (field.Covers(tile)) AddTile(layout, field, tile);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddTile(LevelLayout layout, OvergrowthField field, Vector2Int tile)
        {
            int samples = field.SamplesPerTile, side = samples + 1;
            float spacing = layout.TileSize / samples, floor = layout.FloorHeight(tile);
            Vector2Int corner = field.CornerOf(tile);
            int start = _vertices.Count;

            for (int row = 0; row <= samples; row++)
                for (int column = 0; column <= samples; column++)
                {
                    int c = corner.x + column, r = corner.y + row;
                    var vertex = new Vector3((tile.x + (float)column / samples) * layout.TileSize,
                                             floor + field.HeightAt(c, r) + Lift,
                                             (tile.y + (float)row / samples) * layout.TileSize);
                    _vertices.Add(vertex);
                    _normals.Add(NormalAt(field, c, r, spacing));
                    _uvs.Add(new Vector2(vertex.x, vertex.z));
                }

            for (int row = 0; row < samples; row++)
                for (int column = 0; column < samples; column++)
                {
                    int c = corner.x + column, r = corner.y + row;
                    if (field.HeightAt(c, r) <= 0f && field.HeightAt(c + 1, r) <= 0f
                        && field.HeightAt(c, r + 1) <= 0f && field.HeightAt(c + 1, r + 1) <= 0f) continue;

                    int a = start + row * side + column, b = a + 1, d = a + side, e = d + 1;
                    // Clockwise seen from above (Unity's front face), as BoxMeshBuilder's top faces.
                    _triangles.Add(a); _triangles.Add(e); _triangles.Add(b);
                    _triangles.Add(a); _triangles.Add(d); _triangles.Add(e);
                }
        }

        // From the slope across the samples either side.
        private static Vector3 NormalAt(OvergrowthField field, int column, int row, float spacing)
        {
            float alongX = (field.HeightAt(column + 1, row) - field.HeightAt(column - 1, row)) / (2f * spacing);
            float alongZ = (field.HeightAt(column, row + 1) - field.HeightAt(column, row - 1)) / (2f * spacing);
            return new Vector3(-alongX, 1f, -alongZ).normalized;
        }
    }
}
