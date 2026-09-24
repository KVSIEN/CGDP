using System;
using UnityEngine;

namespace CGD.WorldMap
{
    // Which parts of the map the player has seen, as a grid of cells over the map area.
    // Also keeps a ready-to-upload pixel per cell (fog colour where unexplored, clear
    // where explored) so the fog texture updates without rebuilding anything.
    public class FogOfWar
    {
        private readonly bool[]    _explored;
        private readonly Color32[] _pixels;
        private readonly Color32   _fog;

        public FogOfWar(int width, int height, Color32 fogColor)
        {
            Width    = Math.Max(1, width);
            Height   = Math.Max(1, height);
            _fog     = fogColor;
            _explored = new bool[Width * Height];
            _pixels   = new Color32[Width * Height];
            Array.Fill(_pixels, _fog);
        }

        public int Width  { get; }
        public int Height { get; }
        public int ExploredCount { get; private set; }

        // 0..1 share of the map uncovered.
        public float ExploredFraction => (float)ExploredCount / _explored.Length;

        public Color32[] Pixels => _pixels;

        // Set whenever cells were revealed since the last ClearDirty — upload then.
        public bool IsDirty { get; private set; }

        public bool IsExplored(Vector2 normalized)
        {
            int x = Mathf.FloorToInt(normalized.x * Width);
            int y = Mathf.FloorToInt(normalized.y * Height);
            return x >= 0 && y >= 0 && x < Width && y < Height && _explored[y * Width + x];
        }

        // Reveals every cell whose centre lies within an ellipse (radius in map space per
        // axis, since cells needn't be square in world terms). Returns true if anything
        // new was revealed.
        public bool Reveal(Vector2 center, Vector2 radius)
        {
            if (radius.x <= 0f || radius.y <= 0f) return false;

            int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius.x) * Width));
            int maxX = Mathf.Min(Width - 1, Mathf.FloorToInt((center.x + radius.x) * Width));
            int minY = Mathf.Max(0, Mathf.FloorToInt((center.y - radius.y) * Height));
            int maxY = Mathf.Min(Height - 1, Mathf.FloorToInt((center.y + radius.y) * Height));

            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            {
                float dy = ((y + 0.5f) / Height - center.y) / radius.y;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = ((x + 0.5f) / Width - center.x) / radius.x;
                    if (dx * dx + dy * dy > 1f) continue;

                    changed |= Set(y * Width + x);
                }
            }

            IsDirty |= changed;
            return changed;
        }

        public void RevealAll()
        {
            for (int i = 0; i < _explored.Length; i++)
                IsDirty |= Set(i);
        }

        public void ClearDirty() => IsDirty = false;

        // One byte per 8 cells, for saving progress.
        public byte[] Export()
        {
            var bits = new byte[(_explored.Length + 7) / 8];
            for (int i = 0; i < _explored.Length; i++)
                if (_explored[i]) bits[i >> 3] |= (byte)(1 << (i & 7));
            return bits;
        }

        public void Import(byte[] bits)
        {
            if (bits == null) return;

            for (int i = 0; i < _explored.Length && (i >> 3) < bits.Length; i++)
                if ((bits[i >> 3] & (1 << (i & 7))) != 0)
                    IsDirty |= Set(i);
        }

        private bool Set(int index)
        {
            if (_explored[index]) return false;

            _explored[index] = true;
            _pixels[index]   = new Color32(_fog.r, _fog.g, _fog.b, 0);
            ExploredCount++;
            return true;
        }
    }
}
