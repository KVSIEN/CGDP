using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A piece of a room's floor at another level, and the stairs (or slopes) between it and the
    // floor round it. `Railed`: its open edges carry a railing, so nobody drops off them (or into
    // it). `Corners`: a mound's or crater's corner pieces, where its slopes meet.
    public sealed class ElevatedArea
    {
        public ElevatedArea(ElevationKind kind, int level, IReadOnlyList<Vector2Int> tiles, IReadOnlyList<StairFlight> stairs, bool railed,
                            IReadOnlyList<SlopeCorner> corners = null)
        {
            Kind    = kind;
            Level   = level;
            Tiles   = tiles;
            Stairs  = stairs;
            Railed  = railed;
            Corners = corners ?? System.Array.Empty<SlopeCorner>();
        }

        public IReadOnlyList<SlopeCorner> Corners { get; }

        public ElevationKind               Kind   { get; }
        public int                         Level  { get; }
        public IReadOnlyList<Vector2Int>   Tiles  { get; }
        public IReadOnlyList<StairFlight>  Stairs { get; }
        public bool                        Railed { get; }
    }
}
