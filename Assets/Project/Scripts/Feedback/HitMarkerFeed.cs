using System;
using CGD.UI;

namespace CGD.Feedback
{
    // Hit confirmations for the crosshair. CombatFeedback raises them for hits the Player
    // deals; HitMarkerHUD listens. No scene reference needed on either side, so the marker can
    // live in the HUD prefab while CombatFeedback lives on the Player prefab.
    public static class HitMarkerFeed
    {
        public static event Action<HitMarkerHUD.Kind> Raised;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Raised = null;

        public static void Show(HitMarkerHUD.Kind kind) => Raised?.Invoke(kind);
    }
}
