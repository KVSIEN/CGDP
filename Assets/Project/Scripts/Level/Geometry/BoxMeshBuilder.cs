using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CGD.Level
{
    // Accumulates axis-aligned boxes into one mesh, so a whole level's walls are a single
    // renderer and collider instead of thousands of cube GameObjects. UVs are in metres,
    // which keeps grid/prototype textures the same scale on every face.
    public class BoxMeshBuilder
    {
        private readonly List<Vector3> _vertices  = new();
        private readonly List<Vector3> _normals   = new();
        private readonly List<Vector2> _uvs       = new();
        private readonly List<int>     _triangles = new();

        public bool IsEmpty => _vertices.Count == 0;

        public void AddBox(Vector3 center, Vector3 size) => AddBox(center, size, Quaternion.identity);

        // A box turned about its centre (diagonal walls).
        public void AddBox(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 h = size * 0.5f;
            Vector3 up = rotation * Vector3.up, right = rotation * Vector3.right, forward = rotation * Vector3.forward;
            AddFace(center,  up,      right,    forward, h.y, h.x, h.z);
            AddFace(center, -up,      right,   -forward, h.y, h.x, h.z);
            AddFace(center,  right,   forward,  up,      h.x, h.z, h.y);
            AddFace(center, -right,  -forward,  up,      h.x, h.z, h.y);
            AddFace(center,  forward, -right,   up,      h.z, h.x, h.y);
            AddFace(center, -forward,  right,   up,      h.z, h.x, h.y);
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

        // One quad: `normal` faces out, `u`/`v` span it; the half-extents are measured along
        // the normal, u and v respectively.
        private void AddFace(Vector3 center, Vector3 normal, Vector3 u, Vector3 v, float depth, float halfU, float halfV)
        {
            Vector3 faceCenter = center + normal * depth;
            int start = _vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                float su = i == 1 || i == 2 ? 1f : -1f;
                float sv = i >= 2 ? 1f : -1f;
                Vector3 vertex = faceCenter + u * (su * halfU) + v * (sv * halfV);
                _vertices.Add(vertex);
                _normals.Add(normal);
                _uvs.Add(new Vector2(Vector3.Dot(vertex, u), Vector3.Dot(vertex, v)));
            }

            // Clockwise seen from outside (Unity's front face).
            _triangles.Add(start);     _triangles.Add(start + 2); _triangles.Add(start + 1);
            _triangles.Add(start);     _triangles.Add(start + 3); _triangles.Add(start + 2);
        }
    }
}
