using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Feedback;
using CGD.Items;
using CGD.Meters;
using CGD.Perks;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Player
{
    // Sets off the triggered perks of the weapon in hand and of every worn armor piece when
    // something happens: kills, hits and crits, parries, dodges, ability casts, taking damage,
    // being healed, aiming, reloading, using an item, swapping weapons. A weapon's hit
    // triggers only count hits it dealt, and its buffs end when it is swapped away.
    // The perks live on the gear (PerkDispatcher runs them); this only listens for the moments.
    // Passive perks need nothing here: weapons apply theirs to themselves, PlayerEquipment
    // applies armor's.
    [RequireComponent(typeof(PlayerWeaponLoadout))]
    public class PlayerPerks : MonoBehaviour
    {
        [Tooltip("Optional — for the Aim Start trigger")]
        [SerializeField] private PlayerCamera _camera;

        private readonly List<ItemInstance> _worn = new();

        private PlayerWeaponLoadout _loadout;
        private WeaponController    _firearm;
        private MeleeController     _melee;
        private PlayerDodge         _dodge;
        private PlayerAbilities     _abilities;
        private PlayerItemSlots     _items;
        private PlayerEquipment     _equipment;
        private HealthManager       _health;
        private PerkDispatcher      _perks;
        private WeaponItem          _inHand;

        private void Awake()
        {
            _loadout = GetComponent<PlayerWeaponLoadout>();
            TryGetComponent(out _firearm);
            TryGetComponent(out _melee);
            TryGetComponent(out _dodge);
            TryGetComponent(out _abilities);
            TryGetComponent(out _items);
            TryGetComponent(out _equipment);
            TryGetComponent(out _health);
            TryGetComponent(out MeterSet meters);

            var context = new PerkContext(_firearm, GetComponentInParent<CharacterStats>(), _health, meters);
            _perks = new PerkDispatcher(context, () => Random.value);
            _perks.Triggered += perk => FeedbackBus.Notify(perk.DisplayName, NotificationStyle.Success);
        }

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamageDealt;
            _loadout.ActiveChanged   += OnWeaponChanged;
            if (_firearm   != null) { _firearm.ReloadStarted += OnReloadStarted; _firearm.Reloaded += OnReloaded; }
            if (_melee     != null) _melee.Parried          += OnParried;
            if (_dodge     != null) _dodge.Dodged           += OnDodged;
            if (_abilities != null) _abilities.AbilityUsed  += OnAbilityUsed;
            if (_items     != null) _items.UseEnded         += OnItemUsed;
            if (_camera    != null) _camera.AimStarted      += OnAimStarted;
            if (_equipment != null) _equipment.Changed      += RefreshWorn;
            if (_health    != null) { _health.OnHealed += OnHealed; _health.OnRevived += _perks.ResetCooldowns; }
            RefreshWorn();
        }

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamageDealt;
            _loadout.ActiveChanged   -= OnWeaponChanged;
            if (_firearm   != null) { _firearm.ReloadStarted -= OnReloadStarted; _firearm.Reloaded -= OnReloaded; }
            if (_melee     != null) _melee.Parried          -= OnParried;
            if (_dodge     != null) _dodge.Dodged           -= OnDodged;
            if (_abilities != null) _abilities.AbilityUsed  -= OnAbilityUsed;
            if (_items     != null) _items.UseEnded         -= OnItemUsed;
            if (_camera    != null) _camera.AimStarted      -= OnAimStarted;
            if (_equipment != null) _equipment.Changed      -= RefreshWorn;
            if (_health    != null) { _health.OnHealed -= OnHealed; _health.OnRevived -= _perks.ResetCooldowns; }
        }

        // Cached so hit events don't enumerate the equipment dictionary each time.
        private void RefreshWorn()
        {
            _worn.Clear();
            if (_equipment != null) _worn.AddRange(_equipment.Equipment.Worn);
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Amount <= 0f) return;

            if (report.Target == _health)
            {
                Fire(PerkTrigger.DamageTaken);
                return;
            }
            if (report.Source.Owner != gameObject) return;

            // Hits by the weapon in hand count for its perks; any hit counts for armor's (a
            // grenade, an ability, an arrow still flying from the weapon swapped away from).
            object dealtBy = report.Source.Weapon;
            Fire(PerkTrigger.Hit, dealtBy);
            if (report.IsCritical) Fire(PerkTrigger.CriticalHit, dealtBy);
            if (report.Killed)     Fire(PerkTrigger.Kill, dealtBy);
        }

        private void OnWeaponChanged(WeaponItem weapon)
        {
            _perks.Context.EndBuffsFrom(_inHand);
            _inHand = weapon;
            Fire(PerkTrigger.WeaponSwap);
        }

        private void OnParried()                => Fire(PerkTrigger.Parry);
        private void OnDodged()                 => Fire(PerkTrigger.Dodge);
        private void OnAbilityUsed(Ability _)   => Fire(PerkTrigger.AbilityCast);
        private void OnHealed(float _)          => Fire(PerkTrigger.Healed);
        private void OnAimStarted()             => Fire(PerkTrigger.AimStart);
        private void OnReloadStarted()          => Fire(PerkTrigger.ReloadStart);
        private void OnReloaded()               => Fire(PerkTrigger.Reloaded);

        private void OnItemUsed(ConsumableDefinition item, bool completed)
        {
            if (completed) Fire(PerkTrigger.ItemUsed);
        }

        private void Fire(PerkTrigger trigger) => Fire(trigger, _loadout.Active);

        // `dealtBy`: for hit triggers, what dealt the hit; the weapon in hand's perks only run
        // when it was that weapon. Other triggers pass the weapon in hand.
        private void Fire(PerkTrigger trigger, object dealtBy)
        {
            float now = Time.time;
            WeaponItem active = _loadout.Active;
            if (active != null && ReferenceEquals(dealtBy, active))
                _perks.Fire(trigger, active, now);

            for (int i = 0; i < _worn.Count; i++)
                _perks.Fire(trigger, _worn[i], now);
        }
    }
}
