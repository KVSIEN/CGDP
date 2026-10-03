using UnityEngine;
using CGD.Flow;
using CGD.Interaction;

namespace CGD.Level
{
    // The extraction point of a generated level: using it ends the run in victory through
    // GameFlow, which an ExpeditionRunner settles by keeping everything the player carries.
    // LevelBuilder adds a plain one to the Exit room when the room's content doesn't bring its own.
    // An emergency exit (the escape pod in an Emergency Exit room) ends the run the same way,
    // but the runner only lets the player keep part of the haul.
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _label = "Extract";
        [Tooltip("Seconds the Interact key must be held")]
        [SerializeField] private float _holdDuration = 1f;
        [Tooltip("An early way out: the run ends, but only part of what's carried is kept")]
        [SerializeField] private bool _emergency;

        // Raised just before the run ends through an exit, so the run knows which kind it was.
        public static event System.Action<LevelExit> Used;

        public bool IsEmergency => _emergency;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Used = null;

        public float HoldDuration => _holdDuration;

        public string GetInteractLabel(GameObject interactor) => _label;

        public bool CanInteract(GameObject interactor) => GameFlow.Instance != null && GameFlow.Instance.State == GameState.Playing;

        public void Interact(GameObject interactor)
        {
            if (GameFlow.Instance == null) return;
            Used?.Invoke(this);
            GameFlow.Instance.Victory();
        }
    }
}
