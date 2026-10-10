using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // What MapStyleLearner measured in a set of example maps, as generation numbers. A
    // null range or a negative value means the examples said nothing about it (no locked
    // doors to learn a terminal share from, say) and the base settings' value is kept.
    public class LearnedMapStyle
    {
        public const float Keep = -1f;

        public int MapCount;

        // Layout
        public IntRange OptionalRooms;
        public int      MinBossDepth;
        public int      MaxSpread;
        public int      MaxConnectionsPerNode;
        public MapPathDirection Direction;
        public IntRange PathLength;
        public float    PathWinding;
        public IntRange? BranchLength;
        public float    BranchWinding = Keep;
        public float    ForkChance    = Keep;
        public IntRange HubBranches;
        public IntRange LoopCount;
        public IntRange ShortcutCount;
        public float    OneWayShortcutChance = Keep;
        public float    LockedChance;
        public float    SecretChance;
        public int      MaxGates;
        public int      GateMinDepth = -1;
        public float    TerminalLockChance = Keep;
        public IntRange? TerminalCount;
        public float    DirectDoorChance;

        // Content
        public readonly List<LearnedTypeRule> TypeRules = new();
        public int MaxCombatInARow;

        // Categories (only when the examples have any)
        public bool HasCategories;
        public readonly Dictionary<RoomCategory, float> CategoryWeights = new();
        public readonly Dictionary<ShipZone, float>     ZoneShares      = new();
        public readonly List<(RoomCategory a, RoomCategory b, float affinity)> Affinities = new();
        public float SameCategoryAffinity = Keep;

        // What was learned (and skipped), for the person who asked.
        public readonly List<string> Notes = new();
    }
}
