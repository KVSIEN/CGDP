using System;
using UnityEngine;

namespace CGD.Level
{
    // Where a level's search finds lean (SearchLootDistributor): rooms off the way to the boss,
    // and more so behind a gate, are likelier to hold them, so exploring pays more than running
    // the main path. The total stays what the level's searchable furniture adds.
    [Serializable]
    public class SearchLootSettings
    {
        [Tooltip("How much likelier a find is in furniture off the main path than on it")]
        [SerializeField, Min(0f)] private float _offPathWeight = 2f;
        [Tooltip("How much likelier a find is in furniture behind a gate (a locked door, a keycard) than on the main path")]
        [SerializeField, Min(0f)] private float _behindGateWeight = 3f;

        public SearchLootSettings() { }

        public SearchLootSettings(float offPathWeight, float behindGateWeight)
        {
            _offPathWeight    = offPathWeight;
            _behindGateWeight = behindGateWeight;
        }

        // A room's weight: 1 on the main path, else the off-path or behind-gate weight, the larger.
        public float RoomWeight(bool onMainPath, bool behindGate)
        {
            float weight = onMainPath ? 1f : _offPathWeight;
            return behindGate ? Mathf.Max(weight, _behindGateWeight) : weight;
        }
    }
}
