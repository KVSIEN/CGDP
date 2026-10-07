using System;
using UnityEngine;
using CGD.Items;
using CGD.Perks;
using CGD.Stats;

namespace CGD.Player
{
    // The Player's worn armor and attachment fitting. Wearing a piece takes it out of
    // the inventory (the old piece goes back in) and adds its stats to CharacterStats
    // as flat bonuses — armor, health, resistances, whatever it rolled, with its own
    // attachments applied — plus its passive perks (move speed, regen…) as they are authored. Attachments come from the inventory and fit onto worn armor
    // or carried weapons.
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerEquipment : MonoBehaviour
    {
        private PlayerInventory _inventory;
        private CharacterStats  _stats;

        public Equipment Equipment { get; } = new();

        // Anything worn or fitted changed.
        public event Action Changed;

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
            TryGetComponent(out _stats);
        }

        public bool Wear(ItemInstance armor)
        {
            if (!Equipment.CanWear(armor) || !_inventory.Inventory.Remove(armor)) return false;

            ItemInstance previous = Equipment.Wear(armor);
            if (previous != null) TakeOff(previous, backToInventory: true);

            armor.AttachmentsChanged += OnWornAttachmentsChanged;
            ApplyStats(armor);
            Changed?.Invoke();
            return true;
        }

        public bool Unwear(EquipmentSlot slot)
        {
            ItemInstance armor = Equipment.Remove(slot);
            if (armor == null) return false;

            TakeOff(armor, backToInventory: true);
            Changed?.Invoke();
            return true;
        }

        // Moves one attachment from the inventory onto the gear.
        public bool Fit(AttachmentDefinition attachment, ItemInstance gear)
        {
            if (gear == null || !gear.CanHost(attachment)) return false;
            if (!_inventory.Inventory.Remove(attachment, 1)) return false;

            gear.TryAttach(attachment);
            Changed?.Invoke();
            return true;
        }

        public bool Unfit(AttachmentDefinition attachment, ItemInstance gear)
        {
            if (gear == null || !gear.Detach(attachment)) return false;

            _inventory.Inventory.Add(attachment, 1);
            Changed?.Invoke();
            return true;
        }

        private void TakeOff(ItemInstance armor, bool backToInventory)
        {
            armor.AttachmentsChanged -= OnWornAttachmentsChanged;
            if (_stats != null) _stats.RemoveFrom(armor);
            if (backToInventory) _inventory.Inventory.Add(armor);
        }

        // Each piece is the Source of its own modifiers, so re-applying one is remove + add.
        private void ApplyStats(ItemInstance armor)
        {
            if (_stats == null) return;

            _stats.RemoveFrom(armor);
            for (int i = 1; i < ItemStatTraits.Count; i++)
            {
                var stat  = (ItemStat)i;
                float value = armor.GetStat(stat);
                if (!Mathf.Approximately(value, 0f))
                    _stats.Add(stat, StatModifierOp.Additive, value, armor);
            }

            foreach (GearPerk perk in armor.Perks)
            {
                if (perk is not PassivePerk passive) continue;
                foreach (StatModifier modifier in passive.Modifiers)
                    if (modifier.Stat != ItemStat.None)
                        _stats.Add(modifier.Stat, modifier.Op, modifier.Value, armor);
            }
        }

        private void OnWornAttachmentsChanged()
        {
            foreach (ItemInstance armor in Equipment.Worn)
                ApplyStats(armor);
        }
    }
}
