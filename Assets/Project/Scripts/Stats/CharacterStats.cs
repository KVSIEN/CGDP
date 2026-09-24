using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Items;
using CGD.Timing;

namespace CGD.Stats
{
    // The modifiers acting on one character, layered over the base values its own data
    // provides: Apply(Damage, weaponDamage) returns weapon damage with every buff,
    // debuff and difficulty modifier on this character applied.
    //
    //   base (WeaponData, EnemyData…) → equipment (attachments, on the item itself)
    //   → permanent presets (difficulty) → temporary modifiers (buffs, run on game time)
    //
    // Health, weapons and enemy attacks read through this when it's on the character;
    // without it they use their base values unchanged.
    public class CharacterStats : MonoBehaviour, ITickable
    {
        [Tooltip("Always applied, e.g. a difficulty preset for enemies")]
        [SerializeField] private StatModifierPreset[] _presets = System.Array.Empty<StatModifierPreset>();

        private readonly ModifierSet<ItemStat> _modifiers = new();
        private TimedModifiers<ItemStat> _timed;
        private HealthManager _health;
        private bool _initialized;

        public ModifierSet<ItemStat> Modifiers
        {
            get
            {
                EnsureInitialized();
                return _modifiers;
            }
        }

        // Other components may read stats from their own Awake, before ours has run.
        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;
            _timed = new TimedModifiers<ItemStat>(_modifiers);
            foreach (StatModifierPreset preset in _presets)
                AddPreset(preset);
        }

        // Buffs and debuffs end on death-and-revive, like status effects do.
        private void Awake()
        {
            EnsureInitialized();
            if (TryGetComponent(out _health)) _health.OnRevived += ClearTimed;
        }

        private void OnDestroy()
        {
            if (_health != null) _health.OnRevived -= ClearTimed;
        }

        private void OnEnable() => GameTime.Instance.Clock.Register(this);

        private void OnDisable()
        {
            if (GameTime.Exists) GameTime.Instance.Clock.Unregister(this);
        }

        void ITickable.Tick(float deltaTime) => _timed.Tick(deltaTime);

        public float Apply(ItemStat stat, float baseValue) => Modifiers.Apply(stat, baseValue);

        public void Add(ItemStat stat, StatModifierOp op, float value, object source) =>
            Modifiers.Add(stat, new Modifier(op, value, source));

        public int RemoveFrom(object source) => Modifiers.RemoveFrom(source);

        // Permanent until RemoveFrom(preset).
        public void AddPreset(StatModifierPreset preset)
        {
            if (preset == null) return;

            foreach (StatModifier modifier in preset.Modifiers)
                if (modifier.Stat != ItemStat.None)
                    _modifiers.Add(modifier.Stat, new Modifier(modifier.Op, modifier.Value, preset));
        }

        // The whole preset for `seconds` of game time; End() the handle to cancel early.
        public TimedModifier AddTimed(StatModifierPreset preset, float seconds)
        {
            EnsureInitialized();
            return _timed.Add(Entries(preset), seconds);
        }

        public TimedModifier AddTimed(ItemStat stat, StatModifierOp op, float value, float seconds)
        {
            EnsureInitialized();
            return _timed.Add(stat, op, value, seconds);
        }

        // Clears buffs and debuffs (on respawn); presets stay.
        public void ClearTimed()
        {
            EnsureInitialized();
            _timed.Clear();
        }

        private static IEnumerable<(ItemStat, StatModifierOp, float)> Entries(StatModifierPreset preset)
        {
            if (preset == null) yield break;

            foreach (StatModifier modifier in preset.Modifiers)
                if (modifier.Stat != ItemStat.None)
                    yield return (modifier.Stat, modifier.Op, modifier.Value);
        }
    }
}
