using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Quests
{
    // Items granted when a quest completes. Gear is rolled fresh at its own tier.
    [Serializable]
    public struct QuestReward
    {
        public ItemDefinition Item;
        [Min(1)] public int   Count;
    }
}
