using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Expedition
{
    // What the starting room hands the player each run (GDD › Starting Room): always the same
    // kind of starting weapon, freshly rolled, plus random gear and supplies.
    // Random, not tailored to the map: every run starts blind.
    [CreateAssetMenu(fileName = "RunStarterKit", menuName = "CGD/Expedition/Run Starter Kit")]
    public class RunStarterKit : ScriptableObject
    {
        [Tooltip("Handed over every run, rolled anew each time")]
        [SerializeField] private WeaponCategory _startingWeapon;
        [Tooltip("Gear (armor, or more weapons) the run may also start with; no piece twice in one run")]
        [SerializeField] private GearDefinition[] _gear = Array.Empty<GearDefinition>();
        [SerializeField] private IntRange _gearCount = new(0, 1);
        [Tooltip("Consumables, ammunition and crafting materials, each rolled within its count")]
        [SerializeField] private List<StarterSupply> _supplies = new();

        public WeaponCategory StartingWeapon => _startingWeapon;
        public IReadOnlyList<StarterSupply> Supplies => _supplies;

        // GearCount distinct pieces of gear, in random order.
        public List<GearDefinition> PickGear(RandomStream random)
        {
            var pool = new List<GearDefinition>();
            foreach (GearDefinition gear in _gear)
                if (gear != null && !pool.Contains(gear)) pool.Add(gear);

            random.Shuffle(pool);
            int count = Mathf.Clamp(_gearCount.Evaluate(random), 0, pool.Count);
            return pool.GetRange(0, count);
        }
    }
}
