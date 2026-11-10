using UnityEngine;

namespace CGD.UI
{
    // The HUD's palette: every colour a HUD element draws with, named by what it means, not by
    // where it is used. Restyling the HUD is editing one asset. Elements read it through
    // HUDElement.Theme; one left without a theme asset uses the built-in defaults, which are
    // the values below. Colours are opaque unless a surface is meant to be see-through; an
    // element that wants more or less transparency uses Color.WithAlpha.
    [CreateAssetMenu(fileName = "HUDTheme", menuName = "CGD/UI/HUD Theme")]
    public class HUDTheme : ScriptableObject
    {
        private static HUDTheme _fallback;

        // Forgotten at the start of every play session, even when Enter Play Mode
        // skips the domain reload (the last session's fallback was destroyed with it).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _fallback = null;

        // The defaults, for an element that has no theme assigned.
        public static HUDTheme Fallback
        {
            get
            {
                if (_fallback != null) return _fallback;
                _fallback = CreateInstance<HUDTheme>();
                _fallback.hideFlags = HideFlags.HideAndDontSave;
                return _fallback;
            }
        }

        [Header("Surfaces")]
        [Tooltip("Background of the corner HUD panels (health, ammo, speed, quests)")]
        public Color Panel = new(0f, 0f, 0f, 0.45f);
        [Tooltip("A darker see-through surface: slot tiles, notification rows, bar troughs over the scene")]
        public Color Scrim = new(0f, 0f, 0f, 0.6f);
        [Tooltip("Drains over a slot to show a cooldown")]
        public Color Overlay = new(0f, 0f, 0f, 0.65f);
        [Tooltip("Windows and cards drawn over the game (inventory, pickup details, prompts)")]
        public Color Window = new(0.05f, 0.05f, 0.08f, 0.93f);
        [Tooltip("An item slot inside a window")]
        public Color Slot = new(0.12f, 0.12f, 0.18f, 0.88f);
        [Tooltip("A slot that is empty, unavailable or recharging")]
        public Color SlotEmpty = new(0.1f, 0.1f, 0.1f, 0.85f);
        [Tooltip("Trough behind a bar's fill")]
        public Color BarBackground = new(0.08f, 0.08f, 0.08f, 0.9f);
        [Tooltip("Thin divider lines inside windows")]
        public Color Rule = new(1f, 1f, 1f, 0.1f);

        [Header("Text")]
        public Color TextPrimary = new(0.92f, 0.92f, 0.92f, 1f);
        [Tooltip("Secondary labels, empty and unavailable values")]
        public Color TextSecondary = new(0.7f, 0.7f, 0.7f, 1f);
        [Tooltip("Captions, stat names, footers")]
        public Color TextMuted = new(0.55f, 0.55f, 0.55f, 1f);

        [Header("Status")]
        public Color Health = new(0.22f, 0.85f, 0.22f, 1f);
        public Color HealthLow = new(0.9f, 0.15f, 0.1f, 1f);
        [Tooltip("Good news: hidden, success, objective progress, a committed dodge")]
        public Color Positive = new(0.45f, 0.9f, 0.55f, 1f);
        [Tooltip("Caution: reloading, revealed, an effect running out, a move that can be cancelled")]
        public Color Warning = new(1f, 0.75f, 0.2f, 1f);
        [Tooltip("Bad news: spotted, danger, an empty magazine, a kill")]
        public Color Danger = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Rewards and surge")]
        public Color Reward = new(1f, 0.85f, 0.4f, 1f);
        [Tooltip("Critical hits")]
        public Color Critical = new(1f, 0.55f, 0.1f, 1f);
        [Tooltip("Something exhausted or switched off")]
        public Color Disabled = new(0.45f, 0.45f, 0.45f, 1f);

        [Header("Accent")]
        [Tooltip("Ready, selected, highlighted")]
        public Color Accent = new(0.25f, 0.55f, 1f, 1f);
        [Tooltip("The active entry in a list; key badges")]
        public Color AccentStrong = new(0.22f, 0.42f, 0.82f, 1f);
    }
}
