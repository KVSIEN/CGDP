using System;
using CGD.Core;
using CGD.Items;

namespace CGD.Expedition
{
    // One line of the starting room's loadout: an item and how many, rolled per run.
    [Serializable]
    public struct StarterSupply
    {
        public ItemDefinition Item;
        public IntRange Count;
    }
}
