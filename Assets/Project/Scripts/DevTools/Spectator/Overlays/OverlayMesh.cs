using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CGD.DevTools
{
    // Debug lines drawn into the Game view for one camera: every line is a thin ribbon
    // turned to face the camera and kept the same width on screen however far away it is,
    // so overlays read like the Scene view's gizmos. Shapes are gathered between Begin and
    // Render, built into one mesh with a colour per vertex, and drawn that frame only.
    // Lists and mesh are reused, so drawing allocates nothing once warmed up.
    internal sealed class OverlayMesh : IDisposable
    {
        private const int CircleSegments = 40;
        private static readonly Bounds Everywhere = new(Vector3.zero, Vector3.one * 100000f);

        private readonly Mesh          _mesh;
        private readonly Material      _material;
        private readonly List<Vector3> _vertices  = new();
        private readonly List<Color32> _colors    = new();
        private readonly List<int>     _triangles = new();

        private Camera  _camera;
        private Vector3 _eye;
        private float   _worldPerPixel;   // at one metre from the camera

        public OverlayMesh(Material material)
        {
            _material = material;
            _mesh = new Mesh { name = "Spectator Overlays", indexFormat = IndexFormat.UInt32 };
            _mesh.MarkDynamic();
        }

        public void Begin(Camera camera)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            _camera = camera;
            _eye    = camera.transform.position;
            _worldPerPixel = 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1, camera.pixelHeight);
        }

        public void Render()
        {
            if (_camera == null || _material == null || _vertices.Count == 0) return;

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0, false);
            _mesh.bounds = Everywhere;

            var parameters = new RenderParams(_material)
            {
                camera            = _camera,
                worldBounds       = Everywhere,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows    = false,
            };
            Graphics.RenderMesh(parameters, _mesh, 0, Matrix4x4.identity);
        }

        public void Dispose()
        {
            if (_mesh != null) UnityEngine.Object.Destroy(_mesh);
        }

        // --- Shapes -------------------------------------------------------------------

        public void Line(Vector3 from, Vector3 to, Color color, float pixels = 2f)
        {
            Vector3 along = to - from;
            if (along.sqrMagnitude < 1e-6f) return;
            Vector3 side = Vector3.Cross(along, (from + to) * 0.5f - _eye);
            if (side.sqrMagnitude < 1e-8f) return;
            side.Normalize();

            float halfPerMetre = pixels * 0.5f * _worldPerPixel;
            Vector3 a = side * (halfPerMetre * Vector3.Distance(from, _eye));
            Vector3 b = side * (halfPerMetre * Vector3.Distance(to, _eye));

            int first = _vertices.Count;
            _vertices.Add(from - a);
            _vertices.Add(from + a);
            _vertices.Add(to + b);
            _vertices.Add(to - b);
            Color32 c = color;
            for (int i = 0; i < 4; i++) _colors.Add(c);
            _triangles.Add(first);     _triangles.Add(first + 1); _triangles.Add(first + 2);
            _triangles.Add(first);     _triangles.Add(first + 2); _triangles.Add(first + 3);
        }

        // Dashes of `dash` metres with gaps as long: something believed rather than seen.
        public void Dashed(Vector3 from, Vector3 to, Color color, float pixels = 2f, float dash = 0.5f)
        {
            float length = Vector3.Distance(from, to);
            if (length < 1e-3f) return;
            Vector3 step = (to - from) / length;
            for (float at = 0f; at < length; at += dash * 2f)
                Line(from + step * at, from + step * Mathf.Min(at + dash, length), color, pixels);
        }

        public void Circle(Vector3 center, float radius, Color color, float pixels = 2f) =>
            Arc(center, Vector3.forward, 360f, radius, color, pixels, edges: false);

        // A flat fan on the ground: the arc, and with edges its two sides back to the center.
        public void Arc(Vector3 center, Vector3 forward, float angle, float radius, Color color, float pixels = 2f, bool edges = true)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            int     segments = Mathf.Max(4, Mathf.CeilToInt(CircleSegments * angle / 360f));
            Vector3 previous = center + Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward * radius;
            if (edges) Line(center, previous, color, pixels);
            for (int i = 1; i <= segments; i++)
            {
                float   turn = -angle * 0.5f + angle * i / segments;
                Vector3 next = center + Quaternion.AngleAxis(turn, Vector3.up) * forward * radius;
                Line(previous, next, color, pixels);
                previous = next;
            }
            if (edges) Line(center, previous, color, pixels);
        }

        // An X lying on the ground.
        public void Cross(Vector3 point, float size, Color color, float pixels = 2f)
        {
            Vector3 a = new Vector3(1f, 0f, 1f) * (size * 0.5f);
            Vector3 b = new Vector3(1f, 0f, -1f) * (size * 0.5f);
            Line(point - a, point + a, color, pixels);
            Line(point - b, point + b, color, pixels);
        }

        // A short upright stick, its height telling one thing from another.
        public void Post(Vector3 foot, float height, Color color, float pixels = 2f) =>
            Line(foot, foot + Vector3.up * height, color, pixels);

        // A line with a two-stroke head at `to`.
        public void Arrow(Vector3 from, Vector3 to, Color color, float pixels = 2f)
        {
            Line(from, to, color, pixels);
            Vector3 back = from - to;
            back.y = 0f;
            if (back.sqrMagnitude < 1e-4f) return;
            back = back.normalized * 0.6f;
            Line(to, to + Quaternion.AngleAxis(30f, Vector3.up) * back, color, pixels);
            Line(to, to + Quaternion.AngleAxis(-30f, Vector3.up) * back, color, pixels);
        }
    }
}
