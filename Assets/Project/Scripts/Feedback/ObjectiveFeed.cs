using System;
using UnityEngine;

namespace CGD.Feedback
{
    // The one objective on screen at a time. Encounters Show theirs when it starts and Clear
    // it when it ends; ObjectiveHUD reads Current. No scene reference needed on either side.
    public static class ObjectiveFeed
    {
        public static ObjectiveStatus Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Current = null;
            Changed = null;
        }

        public static event Action Changed;

        public static void Show(ObjectiveStatus status)
        {
            Current = status;
            Changed?.Invoke();
        }

        // Only clears if `status` is still the one showing, so a late Clear from an old
        // encounter can't remove a newer one.
        public static void Clear(ObjectiveStatus status)
        {
            if (Current != status) return;

            Current = null;
            Changed?.Invoke();
        }
    }
}
