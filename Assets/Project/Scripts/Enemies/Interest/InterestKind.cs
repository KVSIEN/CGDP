using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // A kind of thing in the world that draws enemies' attention while they are idle (a patch of
    // grass to graze, a recharging station): which factions like it and how much, how long they
    // stay, how many fit at once, how long before the same enemy comes back, and what it does for
    // them while they are there. Shared by every PointOfInterest of the kind.
    [CreateAssetMenu(fileName = "NewInterestKind", menuName = "CGD/Enemies/Interest Kind")]
    public class InterestKind : ScriptableObject
    {
        [SerializeField] private string _displayName = "Interest";
        [Tooltip("The factions it draws and how much. A faction not listed ignores it")]
        [SerializeField] private InterestDraw[] _draws;
        [Tooltip("Only these enemy types are drawn (a cleaning bot's dust, not every Tech). Empty = every enemy of a listed faction")]
        [SerializeField] private EnemyData[] _onlyFor;
        [Tooltip("Seconds an enemy stays once it is there")]
        [SerializeField] private FloatRange _duration = new(6f, 12f);
        [Tooltip("How many enemies can use one point at once")]
        [SerializeField, Min(1)] private int _capacity = 1;
        [Tooltip("Seconds before the same enemy goes back to the same point")]
        [SerializeField, Min(0f)] private float _cooldown = 45f;
        [Tooltip("Metres from where it stands that it will set off for one")]
        [SerializeField, Min(1f)] private float _reach = 30f;
        [Tooltip("Said as it starts (needs a line for it in the bark set)")]
        [SerializeField] private BarkKind _bark = BarkKind.None;
        [Tooltip("Health an enemy gains each second it is there")]
        [SerializeField, Min(0f)] private float _healthPerSecond;
        [Tooltip("Shield an enemy gains each second it is there")]
        [SerializeField, Min(0f)] private float _shieldPerSecond;

        public string     DisplayName     => _displayName;
        public FloatRange Duration        => _duration;
        public int        Capacity        => _capacity;
        public float      Cooldown        => _cooldown;
        public float      Reach           => _reach;
        public BarkKind   Bark            => _bark;
        public float      HealthPerSecond => _healthPerSecond;
        public float      ShieldPerSecond => _shieldPerSecond;

        // 0 for a faction (or, where the kind is for particular types, an enemy type) that does not care for it.
        public float AppealTo(Team team, EnemyData data)
        {
            if (_onlyFor != null && _onlyFor.Length > 0 && System.Array.IndexOf(_onlyFor, data) < 0) return 0f;
            return AppealTo(team);
        }

        // 0 for a faction that does not care for it.
        public float AppealTo(Team team)
        {
            if (_draws == null) return 0f;
            foreach (InterestDraw draw in _draws)
                if (draw.Team == team) return Mathf.Clamp01(draw.Appeal);
            return 0f;
        }
    }
}
