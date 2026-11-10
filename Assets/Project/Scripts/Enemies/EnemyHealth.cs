using UnityEngine;
using CGD.Combat;
using CGD.Core;
using CGD.Items;
using CGD.Quests;
using CGD.Stats;
using CGD.UI;

namespace CGD.Enemies
{
    public class EnemyHealth : HealthManager, IPoolable
    {
        [SerializeField] private EnemyData      _data;
        [Tooltip("Shown instead of the data's name: tells apart two of a kind in one scene (Bio Agent 2)")]
        [SerializeField] private string         _displayName;
        [SerializeField] private EnemyHealthBar _healthBar;
        [SerializeField] private Vector3        _popupOffset = new Vector3(0f, 0.3f, 0f);

        private const float RecentDamageHalfLife = 1.5f;   // seconds for the recent-damage share to halve

        private bool  _hasStats;
        private float _recentDamage;
        private float _recentDamageAt;

        // How hard it is being hit right now: health and shield lost lately, as a share of both
        // maxima together, fading by half every RecentDamageHalfLife seconds.
        public float RecentDamageShare =>
            _recentDamage * Mathf.Pow(0.5f, (Time.time - _recentDamageAt) / RecentDamageHalfLife);

        public EnemyData Data => _data;

        // For agents spawned at runtime, so two of a kind can be told apart.
        public void SetDisplayName(string displayName) => _displayName = displayName;

        public override string DisplayName =>
            !string.IsNullOrEmpty(_displayName) ? _displayName :
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
            OnChanged += RefreshHealthBar;
            _hasStats = TryGetComponent<CharacterStats>(out _);
        }

        // Lifesteal (from buffs on this enemy, e.g. Bio's Surge): a share of the damage it deals
        // comes back as health, as PlayerHealth does for the player. Only enemies with a
        // CharacterStats can have any, so only they listen.
        private void OnEnable()
        {
            if (_hasStats) CombatEvents.DamageDealt += OnDamageDealt;
        }

        private void OnDisable()
        {
            if (_hasStats) CombatEvents.DamageDealt -= OnDamageDealt;
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Source.Owner != gameObject || report.Amount <= 0f || report.Target == this || IsDead) return;

            float share = WithModifiers(ItemStat.Lifesteal, 0f);
            if (share > 0f) Heal(report.Amount * share);
        }

        private void RefreshHealthBar()
        {
            if (_healthBar != null) _healthBar.Refresh(Health, MaxHealth, Shield, MaxShield);
        }

        // Kill objectives count enemies by their EnemyData.
        private void ReportKill() => QuestEvents.Report(ObjectiveKind.Kill, _data);

        protected override void OnDamageTaken(float amount, Vector3 point, bool isCritical)
        {
            _recentDamage   = RecentDamageShare + amount / Mathf.Max(1f, MaxHealth + MaxShield);
            _recentDamageAt = Time.time;
            DamageNumbers.Spawn(amount, point + _popupOffset, isCritical, DealerColor(LastHitSource));
            if (_healthBar != null) _healthBar.ShowDamage(Health, MaxHealth, Shield, MaxShield);
        }

        // Damage numbers take the colour of the enemy that dealt the hit, if its data gives one.
        private static Color DealerColor(DamageSource source) =>
            source.Owner != null && source.Owner.TryGetComponent(out EnemyHealth dealer) ? dealer.Data.DamageNumberColor : Color.clear;

        // Swaps the stats this enemy runs on (its rolled affixes) and fills it up to the new maxima.
        public void UseData(EnemyData data)
        {
            _data = data;
            RefillToMax();
        }

        // A pooled enemy comes back at full health; OnRevived lets its other systems reset.
        public void OnSpawned()
        {
            _recentDamage = 0f;
            if (IsDead) Revive();
        }
    }
}
