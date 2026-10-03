using UnityEngine;
using CGD.Flow;
using CGD.Interaction;

namespace CGD.Level
{
    // The extraction point of a generated level: using it ends the run in victory through
    // GameFlow, which an ExpeditionRunner settles by keeping everything the player carries.
    // LevelBuilder adds a plain one to the Exit room when the room's content doesn't bring its own.
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _label = "Extract";
        [Tooltip("Seconds the Interact key must be held")]
        [SerializeField] private float _holdDuration = 1f;

        public float HoldDuration => _holdDuration;

        public string GetInteractLabel(GameObject interactor) => _label;

        public bool CanInteract(GameObject interactor) => GameFlow.Instance != null && GameFlow.Instance.State == GameState.Playing;

        public void Interact(GameObject interactor) => GameFlow.Instance?.Victory();
    }
}
