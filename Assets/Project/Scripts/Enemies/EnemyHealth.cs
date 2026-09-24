using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Quests;
using CGD.UI;

namespace CGD.Enemies
{
    public class EnemyHealth : HealthManager, IPoolable
    {
        [SerializeField] private EnemyData      _data;
        [SerializeField] private EnemyHealthBar _healthBar;
        [SerializeField] private Vector3        _popupOffset = new Vector3(0f, 0.3f, 0f);

        public override string DisplayName =>
            string.IsNullOrEmpty(_data.DisplayName) ? base.DisplayName : _data.DisplayName;

        public override Team  Team      => _data.Team;
        public override float MaxHealth => WithModifiers(ItemStat.Health, _data.MaxHealth);
        public override float Armor     => WithModifiers(ItemStat.Armor, _data.Armor);
        public override float MaxShield => _data.MaxShield;
        protected override float ShieldRegenDelay => _data.ShieldRegenDelay;
        protected override float ShieldRegenRate  => _data.ShieldRegenRate;

        protected override Vector3 DefaultHitPoint => transform.position + Vector3.up * 1.5f;

        protected override void Awake()
        {
            base.Awake();
            OnDeath += ReportKill;
        }

        // Kill objectives count enemies by their EnemyData.
        private void ReportKill() => QuestEvents.Report(ObjectiveKind.Kill, _data);

        protected override void OnDamageTaken(float amount, Vector3 point, bool isCritical)
        {
            DamageNumbers.Spawn(amount, point + _popupOffset, isCritical);
            if (_healthBar != null) _healthBar.ShowDamage(Health, MaxHealth);
        }

        // A pooled enemy comes back at full health; OnRevived lets its other systems reset.
        public void OnSpawned()
        {
            if (IsDead) Revive();
        }
    }
}
