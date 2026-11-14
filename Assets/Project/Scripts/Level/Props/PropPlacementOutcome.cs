namespace CGD.Level
{
    // How one RoomPropPlacement fared in a room: how many it asked for, how many went down,
    // and — when fewer did — why, so room tools can explain a bare or crowded room.
    public readonly struct PropPlacementOutcome
    {
        public PropPlacementOutcome(RoomPropPlacement placement, int wanted, int placed, bool hitFillCap, bool relaxed)
        {
            Placement  = placement;
            Wanted     = wanted;
            Placed     = placed;
            HitFillCap = hitFillCap;
            Relaxed    = relaxed;
        }

        public RoomPropPlacement Placement { get; }
        public int  Wanted     { get; }
        public int  Placed     { get; }
        // It stopped because the room's share of floor for props was used up.
        public bool HitFillCap { get; }
        // A Required placement found no tile that suits it and was put down with looser rules.
        public bool Relaxed    { get; }

        public bool IsShort => Placed < Wanted;

        // The first prefab's name, which is what a designer recognises the placement by.
        public string Name
        {
            get
            {
                if (Placement?.Prefabs == null) return "(none)";
                foreach (var prefab in Placement.Prefabs)
                    if (prefab != null) return prefab.name;
                return "(no prefab)";
            }
        }

        public string Explanation
        {
            get
            {
                if (Relaxed)    return "no tile suited it, so its placement rules were loosened";
                if (!IsShort)   return "all placed";
                if (HitFillCap) return "the room's share of floor for props was used up";
                return Placed == 0 ? "no tile matched its rules (tags, zone, spacing, clearance)" : "ran out of tiles that match its rules";
            }
        }
    }
}
