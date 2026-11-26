using UnityEngine;

namespace CGD.Interaction
{
    // The button beside a button door's frame (one on each side): press E on it to open the
    // door, or close it early. It shows what still bars the door (a key, a condition, a bar).
    public class DoorButton : MonoBehaviour, IInteractable
    {
        [Tooltip("The door it works. Empty = the Door on a parent")]
        [SerializeField] private Door _door;

        private void Awake()
        {
            if (_door == null) _door = GetComponentInParent<Door>();
        }

        public string GetInteractLabel(GameObject interactor) => _door.GetButtonLabel(interactor);
        public void   Interact(GameObject interactor)         => _door.PressButton(interactor);
        public bool   CanInteract(GameObject interactor)      => _door != null;
    }
}
