using System;
using UnityEngine;
using CGD.Items;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Player
{
    // The Player's offhand slot: a one-handed melee weapon or a shield, taken out of the
    // inventory while held. It only counts while the weapon in hand is one-handed (or the
    // hands are empty); then the MeleeController uses it on the Melee key (and a shield's
    // guard), and its main-hand penalty (spread, recoil…) goes onto CharacterStats.
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerOffhand : MonoBehaviour
    {
        private PlayerInventory     _inventory;
        private PlayerWeaponLoadout _loadout;
        private MeleeController     _melee;
        private CharacterStats      _stats;

        public ItemInstance Item { get; private set; }
        // Held, and the main hand leaves room for it.
        public bool IsActive { get; private set; }

        // The held item, or whether it is in use, changed.
        public event Action Changed;

        public static bool CanHold(ItemInstance item) =>
            item is ShieldInstance || (item is MeleeWeaponInstance melee && melee.IsOneHanded);

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            TryGetComponent(out _loadout);
            TryGetComponent(out _melee);
            _stats = GetComponentInParent<CharacterStats>();
        }

        private void OnEnable()
        {
            if (_loadout != null) _loadout.ActiveChanged += OnMainHandChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_loadout != null) _loadout.ActiveChanged -= OnMainHandChanged;
        }

        // Moves the item from the inventory into the offhand; the one held before goes back.
        public bool Hold(ItemInstance item)
        {
            if (!CanHold(item) || !_inventory.Inventory.Remove(item)) return false;

            ItemInstance previous = Item;
            Item = item;
            if (previous != null) _inventory.Inventory.Add(previous);
            Refresh();
            return true;
        }

        public bool Release()
        {
            if (Item == null) return false;

            _inventory.Inventory.Add(Item);
            Item = null;
            Refresh();
            return true;
        }

        // Takes the item away without returning it to the inventory (the run is settled with it).
        public ItemInstance Clear()
        {
            ItemInstance item = Item;
            Item = null;
            Refresh();
            return item;
        }

        private void OnMainHandChanged(WeaponItem _) => Refresh();

        private void Refresh()
        {
            WeaponItem mainHand = _loadout != null ? _loadout.Active : null;
            IsActive = Item != null && (mainHand == null || mainHand.IsOneHanded);
            var offhand = IsActive ? (IOffhand)Item : null;

            if (_stats != null)
            {
                _stats.RemoveFrom(this);
                if (offhand != null)
                    foreach (StatModifier modifier in offhand.MainHandPenalty)
                        if (modifier.Stat != ItemStat.None)
                            _stats.Add(modifier.Stat, modifier.Op, modifier.Value, this);
            }

            if (_melee != null) _melee.SetOffhand(offhand);
            Changed?.Invoke();
        }
    }
}
