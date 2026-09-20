using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Items
{
    // Holds both item families side by side: stackable definitions as counted
    // stacks, unique gear as instances. Plain C# and Unity-independent so the same
    // type serves the run inventory, the docked ship's storage and a loot container.
    //
    // No capacity limit — the GDD doesn't specify one, and the extraction loop's
    // tension comes from what you risk bringing in, not from pack space.
    public class Inventory
    {
        private readonly List<ItemStack>    _stacks = new();
        private readonly List<ItemInstance> _items  = new();

        public IReadOnlyList<ItemStack>    Stacks => _stacks;
        public IReadOnlyList<ItemInstance> Items  => _items;

        // One stack = one slot regardless of its count; each unique gear piece is
        // one slot too. Matches how the HUD lists things — one line per entry.
        public int SlotCount => _stacks.Count + _items.Count;

        // Stackable weight is per-unit × count; gear weight is per-instance.
        public float TotalWeight
        {
            get
            {
                float total = 0f;
                foreach (ItemStack stack in _stacks)
                    if (stack.Definition != null) total += stack.Definition.Weight * stack.Count;
                foreach (ItemInstance item in _items)
                    total += item.Weight;
                return total;
            }
        }

        public event Action Changed;

        // Adds to existing stacks first, then opens new ones, spilling into as many
        // stacks as the count needs. Returns the amount that could not be added,
        // which is 0 unless the call itself was invalid.
        public int Add(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0) return count;

            int remaining = count;

            for (int i = 0; i < _stacks.Count && remaining > 0; i++)
            {
                if (_stacks[i].Definition != definition) continue;

                int space = _stacks[i].SpaceRemaining;
                if (space <= 0) continue;

                int moved = Mathf.Min(space, remaining);
                _stacks[i] = _stacks[i].WithCount(_stacks[i].Count + moved);
                remaining -= moved;
            }

            while (remaining > 0)
            {
                int moved = Mathf.Min(definition.MaxStack, remaining);
                _stacks.Add(new ItemStack(definition, moved));
                remaining -= moved;
            }

            Changed?.Invoke();
            return remaining;
        }

        public void Add(ItemInstance instance)
        {
            if (instance == null) return;

            _items.Add(instance);
            Changed?.Invoke();
        }

        // Removes across however many stacks it takes. All-or-nothing: if the
        // inventory can't cover the full count, nothing is removed.
        public bool Remove(ItemDefinition definition, int count)
        {
            if (definition == null || count <= 0 || CountOf(definition) < count) return false;

            int remaining = count;

            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_stacks[i].Definition != definition) continue;

                int taken = Mathf.Min(_stacks[i].Count, remaining);
                remaining -= taken;

                int left = _stacks[i].Count - taken;
                if (left > 0) _stacks[i] = _stacks[i].WithCount(left);
                else          _stacks.RemoveAt(i);
            }

            Changed?.Invoke();
            return true;
        }

        public bool Remove(ItemInstance instance)
        {
            if (instance == null || !_items.Remove(instance)) return false;

            Changed?.Invoke();
            return true;
        }

        public int CountOf(ItemDefinition definition)
        {
            if (definition == null) return 0;

            int total = 0;
            foreach (ItemStack stack in _stacks)
                if (stack.Definition == definition) total += stack.Count;

            return total;
        }

        // Sums every munition stack with the matching AmmoType, so weapons can ask
        // "how many rounds of my caliber do I have?" without knowing which specific
        // MunitionDefinition asset(s) fed the pool.
        public int CountOf(AmmoType ammoType)
        {
            if (ammoType == AmmoType.None || ammoType == AmmoType.Cooldown) return 0;

            int total = 0;
            foreach (ItemStack stack in _stacks)
                if (stack.Definition is MunitionDefinition m && m.AmmoType == ammoType) total += stack.Count;

            return total;
        }

        // Removes rounds of a given ammo type across whatever stacks it takes.
        // All-or-nothing, same as the ItemDefinition overload.
        public bool Remove(AmmoType ammoType, int count)
        {
            if (count <= 0 || CountOf(ammoType) < count) return false;

            int remaining = count;
            for (int i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (_stacks[i].Definition is not MunitionDefinition m || m.AmmoType != ammoType) continue;

                int taken = Mathf.Min(_stacks[i].Count, remaining);
                remaining -= taken;

                int left = _stacks[i].Count - taken;
                if (left > 0) _stacks[i] = _stacks[i].WithCount(left);
                else          _stacks.RemoveAt(i);
            }

            Changed?.Invoke();
            return true;
        }

        public bool Has(ItemDefinition definition, int count) => CountOf(definition) >= count;

        public void Clear()
        {
            if (_stacks.Count == 0 && _items.Count == 0) return;

            _stacks.Clear();
            _items.Clear();
            Changed?.Invoke();
        }
    }
}
