using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Player
{
    // MonoBehaviour wrapper around plain-C# Inventory for GetComponent access.
    // StartingStacks seeds the runtime Inventory; MaxSlots/MaxWeight are advisory (HUD only).
    public class PlayerInventory : MonoBehaviour
    {
        // Stackables only. Gear needs an ItemRoll to instantiate, and starting
        // weapons already belong to PlayerWeaponLoadout.
        [Serializable]
        public struct StartingStack
        {
            public ItemDefinition Definition;
            [Min(1)] public int   Count;
        }

        [Header("Starting contents")]
        [Tooltip("Stackable items granted on Awake. Munitions here are what the player spawns with.")]
        [SerializeField] private StartingStack[] _startingStacks = Array.Empty<StartingStack>();

        [Header("Advisory caps (HUD readout only, not enforced)")]
        [SerializeField, Min(1)]   private int   _maxSlots  = 24;
        [SerializeField, Min(0.1f)] private float _maxWeight = 50f;

        public Inventory Inventory { get; } = new();

        public int   MaxSlots  => _maxSlots;
        public float MaxWeight => _maxWeight;

        private void Awake() => GrantStartingStacks();

        private void GrantStartingStacks()
        {
            foreach (StartingStack stack in _startingStacks)
            {
                if (stack.Definition == null || stack.Count <= 0) continue;
                Inventory.Add(stack.Definition, stack.Count);
            }
        }
    }
}
