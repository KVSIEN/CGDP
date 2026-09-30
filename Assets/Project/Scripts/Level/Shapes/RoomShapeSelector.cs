using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // Picks and builds the floor plan for one room: a weighted pick from the shapes its room
    // type allows that suit its number of connections, retried until the result is playable
    // and has a doorway spot for every connection. Falls back to a plain rectangle.
    public class RoomShapeSelector
    {
        private const int Attempts = 8;

        private readonly LevelBuildSettings _settings;
        private readonly List<RoomShape> _candidates = new();

        public RoomShapeSelector(LevelBuildSettings settings) => _settings = settings;

        public RoomFootprint Build(MapNode node, int connections, RectInt area, RandomStream random)
        {
            _candidates.Clear();
            foreach (RoomShape shape in _settings.ShapesFor(node.Type))
                if (shape != null && shape.Weight > 0f && shape.Suits(connections))
                    _candidates.Add(shape);

            for (int attempt = 0; attempt < Attempts && _candidates.Count > 0; attempt++)
            {
                RoomFootprint footprint = RoomShapeRasterizer.Rasterize(PickWeighted(random), area, random);
                if (RoomFootprintValidator.IsValid(footprint, _settings.MinRoomWidthTiles) && footprint.Sockets.Count >= connections)
                    return footprint;
            }
            return RoomFootprint.Rectangle(area);
        }

        private RoomShape PickWeighted(RandomStream random)
        {
            float total = 0f;
            foreach (RoomShape shape in _candidates) total += shape.Weight;

            float roll = random.Range(0f, total);
            foreach (RoomShape shape in _candidates)
            {
                roll -= shape.Weight;
                if (roll < 0f) return shape;
            }
            return _candidates[_candidates.Count - 1];
        }
    }
}
