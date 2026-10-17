using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Items;
using CGD.Player;
using CGD.Stats;

namespace CGD.Weapons
{
    // One swing at a time: wind-up, the strike itself, then recovery, with the weapon's stats
    // and the wielder's buffs applied to its speed and damage. MeleeController decides what
    // swings and when (input, combos, buffering, stamina); this runs the swing it starts.
    public sealed class MeleeSwing
    {
        private enum Phase { Idle, Windup, Active, Recovery }

        private readonly MeleeStrike    _strike = new();
        private readonly Transform      _self;
        private readonly PlayerCamera   _camera;
        private readonly CharacterStats _stats;
        private readonly DamageSource   _ownerSource;
        private readonly bool           _debugDraw;
        private readonly float          _debugDuration;

        private Phase _phase;
        private float _phaseTimer;
        private MeleeAttackStep     _activeStep;
        private MeleeWeaponData     _data;
        private MeleeWeaponInstance _weapon;
        private DamageSource        _source;
        private bool                _isBash;

        public MeleeSwing(Transform self, PlayerCamera camera, CharacterStats stats, DamageSource ownerSource,
                          bool debugDraw, float debugDuration)
        {
            _self          = self;
            _camera        = camera;
            _stats         = stats;
            _ownerSource   = ownerSource;
            _debugDraw     = debugDraw;
            _debugDuration = debugDuration;
        }

        public bool IsIdle => _phase == Phase.Idle;

        // The tail of a swing's recovery (from its CancelFrom point) can be cut short.
        public bool InCancelWindow =>
            _phase == Phase.Recovery && _phaseTimer <= _activeStep.RecoveryTime / Speed * (1f - _activeStep.CancelFrom);

        // Divides wind-up, strike and recovery; a timeline-driven strike keeps its authored frames.
        private float Speed => Mathf.Max(0.1f, Stat(ItemStat.FireRate, _data.AttackSpeed));

        // A value through the swinging weapon (its passive perks and attachments), then the
        // wielder's buffs and debuffs. Bashes with fists, guns and shields have only the wielder's.
        private float Stat(ItemStat stat, float value)
        {
            if (_weapon != null) value = _weapon.Modify(stat, value);
            return _stats != null ? _stats.Apply(stat, value) : value;
        }

        // `weapon` is the melee weapon swinging (null for a bash with fists, a gun or a shield);
        // kills are credited to `credit`, which for a bash is the gun in hand.
        public void Begin(MeleeWeaponData data, MeleeWeaponInstance weapon, WeaponItem credit, MeleeAttackStep step, bool bash)
        {
            _data       = data;
            _weapon     = weapon;
            _isBash     = bash;
            _source     = _ownerSource.WithWeapon(credit);
            _activeStep = step;
            _phase      = Phase.Windup;
            _phaseTimer = step.WindupTime / Speed;

            step.SwingSound.TryPlay(_self.position);
            Noise.Emit(_self.position, data.NoiseRadius, _source);
        }

        // Advances the swing. True once the recovery has run out: the caller then starts a
        // buffered swing or calls Finish.
        public bool Tick(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return false;

            switch (_phase)
            {
                case Phase.Windup:
                    EnterActive();
                    break;

                case Phase.Active:
                    if (!_strike.UsesTimeline) ExitActive();
                    break;

                case Phase.Recovery:
                    return true;
            }
            return false;
        }

        // Physics step: shapes and timeline strikes sweep for hits.
        public void FixedTick()
        {
            if (_phase != Phase.Active || _activeStep == null) return;
            if (!_strike.Tick(_camera.transform, _debugDraw, _debugDuration)) ExitActive();
        }

        // The swing is over and nothing follows it.
        public void Finish(float now)
        {
            _phase = Phase.Idle;
            // A bash leaves the combo where it was, like raising the guard.
            if (!_isBash && _weapon != null) _weapon.Combo.Release(now, _data.ComboResetTime);
        }

        // Ends the swing under way (a cancel, dodge or weapon swap). Hits already dealt stand.
        public void Interrupt(bool keepCombo, float now)
        {
            if (_phase == Phase.Active) _strike.End();

            _phase      = Phase.Idle;
            _activeStep = null;

            if (_weapon == null) return;
            if (keepCombo) _weapon.Combo.Release(now, _data.WeaveWindow);
            else           _weapon.Combo.Reset();
        }

        private void EnterActive()
        {
            _phase = Phase.Active;
            Transform aim = _camera.transform;

            if (_activeStep.Timeline != null)
            {
                _phaseTimer = _strike.BeginTimeline(_activeStep.Timeline, aim, _self.root, _source,
                                                    DamageScale(_activeStep.Damage), _debugDraw, _debugDuration);
                return;
            }

            _phaseTimer = _activeStep.ActiveTime / Speed;

            bool crit = Random.value < Stat(ItemStat.CritChance, 0f);
            var info = new DamageInfo(
                Stat(ItemStat.Damage, _activeStep.Damage),
                Mathf.Clamp01(Stat(ItemStat.ArmorPenetration, _activeStep.ArmorPenetration)),
                _activeStep.DamageType,
                Mathf.Max(1f, Stat(ItemStat.CritDamage, _activeStep.CriticalMultiplier)),
                _source,
                _activeStep.OnHitEffects,
                bonuses: new HitBonuses(crit, Stat(ItemStat.StatusChance, 0f), Stat(ItemStat.StatusDamage, 0f)));

            float reach = Mathf.Max(0.1f, Stat(ItemStat.Range, 1f));
            _strike.BeginShapes(_activeStep, info, _data.HitMask, _self.root, reach);
        }

        // A timeline strike authors its own damage per event, so buffs reach it as a multiplier,
        // measured on the step's damage so flat bonuses keep their size.
        private float DamageScale(float stepDamage) =>
            stepDamage > 0f ? Stat(ItemStat.Damage, stepDamage) / stepDamage : 1f;

        private void ExitActive()
        {
            if (_strike.End()) _activeStep.HitSound.TryPlay(_self.position);

            _phase      = Phase.Recovery;
            _phaseTimer = _activeStep.RecoveryTime / Speed;
        }
    }
}
