using UnityEngine;

namespace CGD.Enemies
{
    // One colour per awareness level, shared by every debug view of an enemy's senses (the
    // Scene view's sight gizmo, the spectator's senses overlay), so they read the same.
    public static class AwarenessColors
    {
        public static readonly Color Unaware    = new(0.3f, 0.8f, 1f);
        public static readonly Color Suspicious = new(1f, 0.85f, 0.2f);
        public static readonly Color Searching  = new(1f, 0.5f, 0.1f);
        public static readonly Color Engaged    = new(1f, 0.15f, 0.1f);

        public static Color Of(AwarenessLevel level) => level switch
        {
            AwarenessLevel.Suspicious => Suspicious,
            AwarenessLevel.Searching  => Searching,
            AwarenessLevel.Engaged    => Engaged,
            _                         => Unaware,
        };
    }
}
