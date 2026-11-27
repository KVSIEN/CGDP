using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A piece of a room's floor at another level, and the stairs (or slopes) between it and the
    // floor round it. `Guards`: what stands along its ledges, side by side (LedgeGuard); a guarded
    // ledge is crossed by nobody, so its stairs are the way on and off. `Corners`: a mound's or
    // crater's corner pieces, where its slopes meet, or a platform's corners cut at 45° (GroundCorner).
    public sealed class ElevatedArea
    {
        public ElevatedArea(ElevationKind kind, int level, IReadOnlyList<Vector2Int> tiles, IReadOnlyList<StairFlight> stairs, LedgeGuards guards,
                            IReadOnlyList<GroundCorner> corners = null)
        {
            Kind    = kind;
            Level   = level;
            Tiles   = tiles;
            Stairs  = stairs;
            Guards  = guards;
            Corners = corners ?? System.Array.Empty<GroundCorner>();
        }

        public ElevationKind               Kind    { get; }
        public int                         Level   { get; }
        public IReadOnlyList<Vector2Int>   Tiles   { get; }
        public IReadOnlyList<StairFlight>  Stairs  { get; }
        public LedgeGuards                 Guards  { get; }
        public IReadOnlyList<GroundCorner> Corners { get; }

        // The guard on the ledge from `inside`, one of its tiles, to its neighbour `outside`.
        public LedgeGuard GuardOn(Vector2Int inside, Vector2Int outside) => Guards.On(outside - inside);

        // The guard across a cut corner's diagonal (GroundCorner.Sheer): the stronger of the two
        // ledges meeting there (a half wall over a railing over none).
        public LedgeGuard GuardAcross(GroundCorner corner) =>
            (LedgeGuard)Mathf.Max((int)Guards.On(new Vector2Int(-corner.Up.x, 0)), (int)Guards.On(new Vector2Int(0, -corner.Up.y)));
    }
}
