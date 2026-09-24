using System;
using System.Collections.Generic;

namespace CGD.Items
{
    // What a character is wearing: at most one armor piece per EquipmentSlot. Plain C#;
    // PlayerEquipment moves pieces between this and the inventory and applies stats.
    public class Equipment
    {
        private readonly Dictionary<EquipmentSlot, ItemInstance> _worn = new();

        // The slot that changed.
        public event Action<EquipmentSlot> Changed;

        public ItemInstance Get(EquipmentSlot slot) => _worn.TryGetValue(slot, out ItemInstance item) ? item : null;

        public IEnumerable<ItemInstance> Worn => _worn.Values;

        public static bool CanWear(ItemInstance item) => item?.Definition is ArmorDefinition;

        // Puts the piece in its slot and returns whatever was there (null if empty).
        public ItemInstance Wear(ItemInstance item)
        {
            if (!CanWear(item)) throw new ArgumentException("Only armor can be worn.", nameof(item));

            EquipmentSlot slot = ((ArmorDefinition)item.Definition).Slot;
            ItemInstance previous = Get(slot);
            _worn[slot] = item;
            Changed?.Invoke(slot);
            return previous;
        }

        public ItemInstance Remove(EquipmentSlot slot)
        {
            if (!_worn.Remove(slot, out ItemInstance item)) return null;

            Changed?.Invoke(slot);
            return item;
        }
    }
}
