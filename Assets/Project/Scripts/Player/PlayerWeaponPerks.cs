using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Meters;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Player
{
    // Sets off the perks of the weapon in hand: on a kill that weapon dealt, a parry with it,
    // or a dodge while holding it, and ends their buffs when it's swapped away. The perks
    // themselves live on the weapons (PerkDispatcher runs them); this only listens for the moments.
    [RequireComponent(typeof(PlayerWeaponLoadout))]
    public class PlayerWeaponPerks : MonoBehaviour
    {
        private PlayerWeaponLoadout _loadout;
        private MeleeController     _melee;
        private PlayerDodge         _dodge;
        private HealthManager       _health;
        private PerkDispatcher      _perks;

        private void Awake()
        {
            _loadout = GetComponent<PlayerWeaponLoadout>();
            TryGetComponent(out _melee);
            TryGetComponent(out _dodge);
            TryGetComponent(out _health);
            TryGetComponent(out WeaponController firearm);
            TryGetComponent(out MeterSet meters);

            _perks = new PerkDispatcher(new PerkContext(firearm, GetComponentInParent<CharacterStats>(), _health, meters));
            _perks.Triggered += perk => FeedbackBus.Notify(perk.DisplayName, NotificationStyle.Success);
        }

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamageDealt;
            _loadout.ActiveChanged   += OnWeaponChanged;
            if (_melee  != null) _melee.Parried     += OnParried;
            if (_dodge  != null) _dodge.Dodged      += OnDodged;
            if (_health != null) _health.OnRevived  += _perks.ResetCooldowns;
        }

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamageDealt;
            _loadout.ActiveChanged   -= OnWeaponChanged;
            if (_melee  != null) _melee.Parried     -= OnParried;
            if (_dodge  != null) _dodge.Dodged      -= OnDodged;
            if (_health != null) _health.OnRevived  -= _perks.ResetCooldowns;
        }

        // Only kills by the weapon in hand: a grenade, ability or an arrow still in flight from
        // the weapon swapped away from doesn't count.
        private void OnDamageDealt(DamageReport report)
        {
            if (!report.Killed || report.Source.Owner != gameObject) return;

            WeaponItem active = _loadout.Active;
            if (active != null && ReferenceEquals(report.Source.Weapon, active))
                Fire(PerkTrigger.Kill);
        }

        private void OnWeaponChanged(WeaponItem weapon) => _perks.WeaponChanged();
        private void OnParried() => Fire(PerkTrigger.Parry);
        private void OnDodged()  => Fire(PerkTrigger.Dodge);

        private void Fire(PerkTrigger trigger) => _perks.Fire(trigger, _loadout.Active, Time.time);
    }
}
