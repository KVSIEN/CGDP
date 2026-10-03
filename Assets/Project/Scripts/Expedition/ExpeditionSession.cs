using UnityEngine;
using CGD.Flow;

namespace CGD.Expedition
{
    // Keeps the ExpeditionLedger (the ship's hold, the packed kit, the last report) alive across
    // scene loads. Sits on the GameFlow object, which survives every load; reached through
    // Instance like GameFlow. Storage lasts for the play session — saving it to disk is a
    // later step (gear needs an item catalog to be rebuilt).
    [RequireComponent(typeof(GameFlow))]
    public class ExpeditionSession : MonoBehaviour
    {
        public static ExpeditionSession Instance { get; private set; }

        public ExpeditionLedger Ledger { get; } = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        // GameFlow destroys a duplicate's whole object; this just doesn't claim Instance first.
        private void Awake()
        {
            if (Instance != null && Instance != this) return;
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
