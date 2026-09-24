using System;
using UnityEngine;
using CGD.Items;
using CGD.Player;

namespace CGD.Interaction
{
    // "Needs a Red Keycard" / "Costs 3 Scrap": an item the interactor must carry, checked
    // against their PlayerInventory. Leave Item empty for no requirement.
    [Serializable]
    public struct ItemRequirement
    {
        public ItemDefinition Item;
        [Min(1)] public int Count;
        [Tooltip("Remove the items from the inventory when the requirement is used")]
        public bool Consume;

        public bool IsNone => Item == null;

        public string Describe() => Count > 1 ? $"{Item.DisplayName} ×{Count}" : Item.DisplayName;

        public bool IsMetBy(GameObject interactor) =>
            IsNone || (interactor.TryGetComponent(out PlayerInventory inventory) && inventory.Inventory.Has(Item, Mathf.Max(1, Count)));

        // Checks and, when Consume is set, takes the items. False leaves the inventory untouched.
        public bool TryUse(GameObject interactor)
        {
            if (!IsMetBy(interactor)) return false;
            if (IsNone || !Consume) return true;

            return interactor.GetComponent<PlayerInventory>().Inventory.Remove(Item, Mathf.Max(1, Count));
        }
    }
}
