using System;
using UnityEngine;
using CGD.Core;
using CGD.Factions;

namespace CGD.Level
{
    // The enemies a room can field — its faction's tiered rosters, or the room type's own
    // list — and the odds of each enemy tier at the room's tier.
    public class RoomRoster
    {
        public static readonly RoomRoster Empty = new(null, Array.Empty<GameObject>(), default);

        private readonly FactionDefinition _faction;
        private readonly GameObject[]      _fallback;
        private readonly EnemyTierOdds     _odds;

        public RoomRoster(FactionDefinition faction, GameObject[] fallback, EnemyTierOdds odds)
        {
            _faction  = faction;
            _fallback = fallback ?? Array.Empty<GameObject>();
            _odds     = odds;
        }

        public bool IsEmpty => OfTier(1).Length == 0;

        // A faction with a roster fields its own enemies; otherwise the room type's (all tier 1).
        public GameObject[] OfTier(int tier) =>
            _faction != null && _faction.Enemies.Length > 0 ? _faction.EnemiesOfTier(tier) : _fallback;

        public GameObject Pick(RandomStream random)
        {
            GameObject[] options = OfTier(_odds.Roll(random));
            return options.Length > 0 ? random.Pick(options) : null;
        }
    }
}
