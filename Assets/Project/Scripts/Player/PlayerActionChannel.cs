using System;
using UnityEngine;

namespace CGD.Player
{
    // Where the player's actions announce themselves, so an action that gives way to others (a
    // reload to a melee bash) listens here once, and asks PlayerActionRules which kinds count,
    // rather than subscribing to every component that might interrupt it. A new action reports
    // here and every rule applies to it.
    // Added to the player by the first component that asks for it (On), so there is nothing to
    // wire in a scene or prefab.
    [DisallowMultipleComponent]
    public class PlayerActionChannel : MonoBehaviour
    {
        public event Action<PlayerActionKind> Started;
        // Only when it went through: a cancelled item use or interrupted cast reports nothing.
        public event Action<PlayerActionKind> Completed;

        public void ReportStarted(PlayerActionKind kind)   => Started?.Invoke(kind);
        public void ReportCompleted(PlayerActionKind kind) => Completed?.Invoke(kind);

        // The player's channel, added on first use. Every caller passes its own GameObject: the
        // player's action components all sit on the one object.
        public static PlayerActionChannel On(GameObject player) =>
            player.TryGetComponent(out PlayerActionChannel channel) ? channel : player.AddComponent<PlayerActionChannel>();
    }
}
