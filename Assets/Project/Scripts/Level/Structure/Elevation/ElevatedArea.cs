using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A piece of a room's floor at another level, and the stairs (or slopes) between it and the
    // floor round it. `Guards`: what stands along its ledges, side by side (LedgeGuard); a guarded
    // ledge is crossed by nobody, so its stairs are the way on and off. `Corners`: a mound's or
    // crater's corner pieces, where its slopes meet.
    public sealed class ElevatedArea
    {
        public ElevatedArea(ElevationKind kind, int level, IReadOnlyList<Vector2Int> tiles, IReadOnlyList<StairFlight> stairs, LedgeGuards guards,
                            IReadOnlyList<SlopeCorner> corners = null)
        {
            Kind    = kind;
            Level   = level;
            Tiles   = tiles;
            Stairs  = stairs;
            Guards  = guards;
            Corners = corners ?? System.Array.Empty<SlopeCorner>();
        }

        public ElevationKind               Kind    { get; }
        public int                         Level   { get; }
        public IReadOnlyList<Vector2Int>   Tiles   { get; }
        public IReadOnlyList<StairFlight>  Stairs  { get; }
        public LedgeGuards                 Guards  { get; }
        public IReadOnlyList<SlopeCorner>  Corners { get; }

        // The guard on the ledge from `inside`, one of its tiles, to its neighbour `outside`.
        public LedgeGuard GuardOn(Vector2Int inside, Vector2Int outside) => Guards.On(outside - inside);
    }
}
