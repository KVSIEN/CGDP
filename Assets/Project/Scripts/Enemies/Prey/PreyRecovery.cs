using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // Prey that heals when predators go for it: a share of every hit a predator team lands on
    // this creature comes back as health over the next few seconds. The herd is hard to wipe
    // out and a predator has to commit to a kill, which keeps the food chain turning instead of
    // draining it. Hits from anyone else (the player, other factions) don't trigger it.
    [RequireComponent(typeof(HealthManager))]
    public class PreyRecovery : MonoBehaviour
    {
        [Tooltip("Teams whose hits set off the recovery. Leave the player out to keep hunting it a plain fight")]
        [SerializeField] private Team[] _predators = { Team.Bio, Team.Void };
        [Tooltip("Share of each predator hit that comes back as health")]
        [SerializeField, Range(0f, 2f)] private float _healShare = 0.6f;
        [Tooltip("Health restored per second while some is owed")]
        [SerializeField, Min(0f)] private float _healPerSecond = 8f;
        [Tooltip("Most health it can be owed at once, as a share of its maximum")]
        [SerializeField, Range(0f, 1f)] private float _owedCap = 0.5f;

        private HealthManager _health;
        private RecoveryPool  _pool;

        private void Awake()
        {
            _health = GetComponent<HealthManager>();
            _pool   = new RecoveryPool(_healShare, _healPerSecond, _owedCap * _health.MaxHealth);
        }

        private void OnEnable()  => CombatEvents.DamageDealt += OnDamageDealt;

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamageDealt;
            _pool?.Clear();
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Target != _health || report.Killed || !IsPredator(report.Source.Team)) return;
            _pool.Bank(report.Amount);
        }

        private bool IsPredator(Team team)
        {
            foreach (Team predator in _predators)
                if (predator == team) return true;
            return false;
        }

        private void Update()
        {
            if (_pool.Owed <= 0f || _health.IsDead) return;

            float paid = _pool.Pay(Time.deltaTime);
            if (paid > 0f) _health.Heal(paid);
        }
    }
}
