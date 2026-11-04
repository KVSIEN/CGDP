using UnityEngine;
using CGD.Enemies;
using CGD.Factions;

namespace CGD.FiringRange
{
    // The firing range's settings, changed by RangeSwitches: which faction's enemies the
    // dummies stand in for, at which tier, whether they sway side to side and how far down
    // the lane they stand. Every dummy gets the first three; only the lane dummies move. A dummy takes on the stats of
    // the chosen enemy, so health, shield, armour and name all follow the enemy assets, and
    // is tinted in the faction's colour. Placeholder tier 2 and 3 affixes are not rolled: a
    // dummy shows the enemy's plain stats.
    public class RangeControl : MonoBehaviour
    {
        private const int MinTier = 1;
        private const int MaxTier = 3;
        private const int MixedFactions = -1;

        [Tooltip("Factions the Faction switch cycles through. Their tier rosters decide which enemy a dummy plays")]
        [SerializeField] private FactionDefinition[] _factions;
        [Tooltip("Dummy 1 plays the first faction, dummy 2 the second … in the Mixed setting")]
        [SerializeField] private RangeDummy[] _dummies;
        [Tooltip("The dummies standing down the lane that the Distance switch moves (the others stay put). This object stands on the firing line, which distances are measured from")]
        [SerializeField] private RangeDummy[] _laneDummies;
        [Tooltip("What the Distance switch cycles through: the share of each lane dummy's placed distance it stands at")]
        [SerializeField] private float[] _distanceScales = { 1f, 0.5f, 0.2f };

        private int  _factionChoice = MixedFactions;
        private int  _tier = MinTier;
        private bool _strafing;
        private int  _distanceChoice;
        private Vector3[] _laneHomes;

        private void Start()
        {
            _laneHomes = new Vector3[_laneDummies.Length];
            for (int i = 0; i < _laneDummies.Length; i++) _laneHomes[i] = _laneDummies[i].transform.position;
            Apply();
        }

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
                case RangeSetting.Distance:
                    _distanceChoice = (_distanceChoice + 1) % _distanceScales.Length;
                    break;
                case RangeSetting.Reset:
                    foreach (RangeDummy dummy in _dummies) dummy.Restore();
                    return;
            }
            Apply();
        }

        // The prompt text of the switch for this setting, with its current value.
        public string Describe(RangeSetting setting) => setting switch
        {
            RangeSetting.Faction  => $"Change faction  ({(_factionChoice == MixedFactions ? "Mixed" : _factions[_factionChoice].DisplayName)})",
            RangeSetting.Tier     => $"Change tier  ({_tier})",
            RangeSetting.Movement => $"Toggle movement  ({(_strafing ? "Swaying" : "Still")})",
            RangeSetting.Distance => $"Change distance  ({DistanceSpan()})",
            RangeSetting.Reset => "Reset dummies",
            _ => string.Empty,
        };

        private void Apply()
        {
            for (int i = 0; i < _dummies.Length; i++)
            {
                FactionDefinition faction = FactionOf(i);
                EnemyData enemy = EnemyFor(faction, _tier);
                if (enemy != null) _dummies[i].Play(enemy, faction.Color);
                _dummies[i].Strafing = _strafing;
            }
            PlaceLaneDummies();
        }

        // Pulls each lane dummy toward the firing line by the chosen share of its distance,
        // keeping its sideways position.
        private void PlaceLaneDummies()
        {
            for (int i = 0; i < _laneDummies.Length; i++)
            {
                float distance = DistanceDownLane(_laneHomes[i]);
                _laneDummies[i].MoveTo(_laneHomes[i] - transform.forward * distance * (1f - _distanceScales[_distanceChoice]));
            }
        }

        private float DistanceDownLane(Vector3 point) => Vector3.Dot(point - transform.position, transform.forward);

        // "25–100 m": the nearest and furthest lane dummy at the current setting.
        private string DistanceSpan()
        {
            if (_laneHomes == null || _laneHomes.Length == 0) return "-";

            float near = float.MaxValue, far = 0f;
            foreach (Vector3 home in _laneHomes)
            {
                float distance = DistanceDownLane(home) * _distanceScales[_distanceChoice];
                near = Mathf.Min(near, distance);
                far  = Mathf.Max(far, distance);
            }
            return $"{near:0.#}–{far:0.#} m";
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
