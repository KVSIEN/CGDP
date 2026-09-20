using System;
using UnityEngine;

namespace CGD.Items
{
    // One "you can't have both" relationship between stats.
    //
    // Each axis rolls a single lean from -1 to +1. Stats in Gains improve as the
    // lean goes positive; stats in Costs suffer by the same amount. That is what
    // makes high damage cost fire rate, or a big magazine cost reload time, instead
    // of every stat rolling independently and occasionally giving away both.
    [Serializable]
    public struct TradeoffAxis
    {
        public string Name;

        [Tooltip("Stats that improve as the roll leans positive")]
        public ItemStat[] Gains;

        [Tooltip("Stats that suffer as the roll leans positive")]
        public ItemStat[] Costs;
    }
}
