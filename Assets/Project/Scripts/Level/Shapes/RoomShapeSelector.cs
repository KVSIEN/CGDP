using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Picks and builds the floor plan for one room: a weighted pick from the given shapes
    // that suit its number of connections, retried until the result is playable and has a
    // doorway spot for every connection. Falls back to a plain rectangle.
    public class RoomShapeSelector
    {
        private const int Attempts = 8;

        private readonly int _minWidthTiles;
        private readonly List<RoomShape> _candidates = new();

        public RoomShapeSelector(int minWidthTiles) => _minWidthTiles = minWidthTiles;

        public RoomFootprint Build(IReadOnlyList<RoomShape> shapes, int connections, RectInt area, RandomStream random)
        {
            _candidates.Clear();
            foreach (RoomShape shape in shapes)
                if (shape != null && shape.Weight > 0f && shape.Suits(connections))
                    _candidates.Add(shape);

            for (int attempt = 0; attempt < Attempts && _candidates.Count > 0; attempt++)
            {
                RoomShape shape = random.PickWeighted(_candidates, s => s.Weight);
                RoomFootprint footprint = RoomShapeRasterizer.Rasterize(shape, area, random);
                if (RoomFootprintValidator.IsValid(footprint, _minWidthTiles) && footprint.Sockets.Count >= connections)
                    return footprint;
            }
            return RoomFootprint.Rectangle(area);
        }
    }
}
