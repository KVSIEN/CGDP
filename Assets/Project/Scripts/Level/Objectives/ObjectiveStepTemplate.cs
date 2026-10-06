using System;
using UnityEngine;

namespace CGD.Level
{
    // One step of an objective template: an action, and which rooms it can be assigned to.
    [Serializable]
    public class ObjectiveStepTemplate
    {
        public ObjectiveAction Action;
        [Tooltip("Room tier the step must be in (0 = any room)")]
        [Range(0, 3)] public int Tier;
        [Tooltip("Shown in the quest log. Empty = generated from the action and the room's tier")]
        public string Description;

        public bool AcceptsTier(int roomTier) => Tier == 0 || Tier == roomTier;
    }
}
