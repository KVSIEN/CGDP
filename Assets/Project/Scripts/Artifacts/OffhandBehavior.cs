using UnityEngine;

namespace CGD.Artifacts
{
    // What an artifact does when used from the offhand — authored once, shared by every artifact
    // that lists it. A new kind of artifact is a new subclass (and its OffhandUse); the slot,
    // input, aim ownership, HUD readout and perks are all handled around it.
    public abstract class OffhandBehavior : ScriptableObject
    {
        [Tooltip("Shown in the Character window, e.g. \"Hold: raise the mirror, deflecting shots\"")]
        [SerializeField, TextArea] private string _usage;

        public string Usage => _usage;

        // A fresh state for each artifact of this behavior.
        public abstract OffhandUse CreateUse();
    }
}
