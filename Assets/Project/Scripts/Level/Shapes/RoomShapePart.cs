using System;
using UnityEngine;

namespace CGD.Level
{
    // One rectangle or ellipse added to or cut out of a room's floor. Positions and sizes are
    // fractions of the room's space — (0, 0) one corner, (1, 1) the opposite — so a shape
    // fits any room size. A part may reach past the edges, e.g. a cut centred on a corner.
    [Serializable]
    public class RoomShapePart
    {
        public RoomShapeOperation Operation;
        public RoomShapeKind Kind;
        public Vector2 Center = new(0.5f, 0.5f);
        [Tooltip("Full width and depth; each build picks a size between SizeMin and SizeMax")]
        public Vector2 SizeMin = Vector2.one;
        public Vector2 SizeMax = Vector2.one;
    }
}
