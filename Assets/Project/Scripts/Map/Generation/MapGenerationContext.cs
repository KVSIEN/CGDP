using System;
using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Shared state for the passes of one MapGenerator run.
    internal class MapGenerationContext
    {
        public MapGenerationContext(MapGenerationSettings settings, int seed)
        {
            Settings = settings;
            Random   = new Random(seed);
        }

        public MapGenerationSettings Settings { get; }
        public MapGraph              Graph    { get; } = new();
        public List<MapSlot>         Slots    { get; } = new();
        public List<string>          Warnings { get; } = new();

        // Seeded so the same seed and settings always give the same map.
        public Random Random { get; }

        public MapSlot GetSlot(int nodeId) => Slots.Find(s => s.NodeId == nodeId);

        public float NextFloat() => (float)Random.NextDouble();

        public bool Chance(float probability) => NextFloat() < probability;

        public int Roll(IntRange range) => range.Lerp(NextFloat());

        public T Pick<T>(IReadOnlyList<T> items) => items[Random.Next(items.Count)];
    }
}
