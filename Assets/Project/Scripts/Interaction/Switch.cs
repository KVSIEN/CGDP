using UnityEngine;

namespace CGD.Interaction
{
    // Attach to a world GameObject with a Collider (set Is Trigger = true).
    // Toggles every linked Door when interacted with.
    [RequireComponent(typeof(Collider))]
    public class Switch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _label = "Switch";
        [SerializeField] private Door[] _doors;
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField] private float _holdDuration = 0f;

        public string GetInteractLabel(GameObject interactor) => _label;
        public float HoldDuration => _holdDuration;

        public void Interact(GameObject interactor)
        {
            foreach (Door door in _doors)
            {
                if (door != null) door.Toggle();
            }
        }
    }
}
