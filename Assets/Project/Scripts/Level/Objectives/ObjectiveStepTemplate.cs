using System;
using UnityEngine;

namespace CGD.Level
{
    // One step of an objective template: an action, and which room tiers it can happen in.
    // A range lets each map roll a different combination (1-1-3, 1-2-2, …).
    [Serializable]
    public class ObjectiveStepTemplate
    {
        public ObjectiveAction Action;
        [Tooltip("Lowest room tier the step can be in")]
        [Range(1, 3)] public int MinTier = 1;
        [Tooltip("Highest room tier the step can be in")]
        [Range(1, 3)] public int MaxTier = 3;
        [Tooltip("Shown in the quest log. Empty = generated from the action and the room's tier")]
        public string Description;

        public bool AcceptsTier(int roomTier) => roomTier >= MinTier && roomTier <= MaxTier;
    }
}
