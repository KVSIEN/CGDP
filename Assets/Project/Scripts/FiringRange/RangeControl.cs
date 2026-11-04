using UnityEngine;
using CGD.Enemies;
using CGD.Factions;

namespace CGD.FiringRange
{
    // The firing range's settings, changed by RangeSwitches: which faction's enemies the
    // dummies stand in for, at which tier, and whether they sway side to side. A dummy
    // takes on the stats of the chosen enemy (EnemyHealth.UseData), so health, shield,
    // armour and name all follow the enemy assets. Placeholder tier 2 and 3 affixes are
    // not rolled: a dummy shows the enemy's plain stats.
    public class RangeControl : MonoBehaviour
    {
        private const int MinTier = 1;
        private const int MaxTier = 3;
        private const int MixedFactions = -1;

        [Tooltip("Factions the Faction switch cycles through. Their tier rosters decide which enemy a dummy plays")]
        [SerializeField] private FactionDefinition[] _factions;
        [Tooltip("Dummy 1 plays the first faction, dummy 2 the second … in the Mixed setting")]
        [SerializeField] private EnemyHealth[] _dummies;

        private StrafeMover[] _movers;
        private int  _factionChoice = MixedFactions;
        private int  _tier = MinTier;
        private bool _strafing;

        private void Awake()
        {
            _movers = new StrafeMover[_dummies.Length];
            for (int i = 0; i < _dummies.Length; i++)
                _dummies[i].TryGetComponent(out _movers[i]);
        }

        private void Start() => Apply();

        public void Cycle(RangeSetting setting)
        {
            switch (setting)
            {
                case RangeSetting.Faction:
                    // Mixed, then each faction, then back to Mixed.
                    _factionChoice = _factionChoice + 1 >= _factions.Length ? MixedFactions : _factionChoice + 1;
                    break;
                case RangeSetting.Tier:
                    _tier = _tier >= MaxTier ? MinTier : _tier + 1;
                    break;
                case RangeSetting.Movement:
                    _strafing = !_strafing;
                    break;
            }
            Apply();
        }

        // The prompt text of the switch for this setting, with its current value.
        public string Describe(RangeSetting setting) => setting switch
        {
            RangeSetting.Faction  => $"Change faction  ({(_factionChoice == MixedFactions ? "Mixed" : _factions[_factionChoice].DisplayName)})",
            RangeSetting.Tier     => $"Change tier  ({_tier})",
            RangeSetting.Movement => $"Toggle movement  ({(_strafing ? "Swaying" : "Still")})",
            _ => string.Empty,
        };

        private void Apply()
        {
            for (int i = 0; i < _dummies.Length; i++)
            {
                EnemyData data = EnemyFor(FactionOf(i), _tier);
                if (data != null) _dummies[i].UseData(data);
                if (_movers[i] != null) _movers[i].Strafing = _strafing;
            }
        }

        private FactionDefinition FactionOf(int dummyIndex)
        {
            if (_factions.Length == 0) return null;
            return _factionChoice == MixedFactions ? _factions[dummyIndex % _factions.Length] : _factions[_factionChoice];
        }

        // The stats of the first enemy on the faction's roster for the tier.
        private static EnemyData EnemyFor(FactionDefinition faction, int tier)
        {
            if (faction == null) return null;

            foreach (GameObject prefab in faction.EnemiesOfTier(tier))
            {
                if (prefab != null && prefab.TryGetComponent(out EnemyHealth health) && health.Data != null)
                    return health.Data;
            }
            return null;
        }
    }
}
