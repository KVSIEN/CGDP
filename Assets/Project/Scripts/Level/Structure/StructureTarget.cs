using UnityEngine;

namespace CGD.Level
{
    // What the structure budget asks of one room, relative to its settings: how far its floor may
    // see before it counts as open (OpenRunScale) and how much structure it should have for its
    // size (PointsScale), and how many more additions than the settings' it may take to get there
    // (ExtraAdditions), and the cover it must hold above (CoverAbove: structure points per floor
    // tile, for a set piece the most covered fight beside it; 0 = none). The map director sets
    // these by the room's beat (BeatTuning). Kept as offsets, so default(StructureTarget) asks
    // exactly what the settings do.
    public readonly struct StructureTarget
    {
        private readonly float _openRunOffset;
        private readonly float _pointsOffset;

        public static StructureTarget Neutral => default;

        public StructureTarget(float openRunScale, float pointsScale, int extraAdditions = 0, float coverAbove = 0f)
        {
            _openRunOffset = Mathf.Max(0.1f, openRunScale) - 1f;
            _pointsOffset  = Mathf.Max(0f, pointsScale) - 1f;
            ExtraAdditions = Mathf.Max(0, extraAdditions);
            CoverAbove     = Mathf.Max(0f, coverAbove);
        }

        // The same target, asking for more cover a tile than `cover`.
        public StructureTarget WithCoverAbove(float cover) => new(OpenRunScale, PointsScale, ExtraAdditions, cover);

        public float OpenRunScale => 1f + _openRunOffset;
        public float PointsScale  => 1f + _pointsOffset;
        public int   ExtraAdditions { get; }
        public float CoverAbove { get; }
    }
}
