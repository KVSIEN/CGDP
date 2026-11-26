using UnityEngine;

namespace CGD.Interaction
{
    // One moving leaf of a Door: a swing leaf (origin on the hinge) or a slide leaf. Put it on
    // the object that carries the leaf's collider, so the player can use the door by looking at
    // any leaf (a handle door's handle, a button door's leaf to force); the Door (on a parent)
    // drives its movement and decides what use does.
    // A door with several panels moves them together: a double door is two swing panels
    // turning opposite ways, a split door two slide panels moving apart.
    public class DoorPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] private DoorMotion _motion = DoorMotion.SlideUp;
        [Tooltip("Degrees for a swing (sign picks the direction), metres for a slide")]
        [SerializeField] private float _amount = 3f;

        private Door       _door;
        private Vector3    _closedPosition;
        private Quaternion _closedRotation;

        private void Awake()
        {
            _door           = GetComponentInParent<Door>();
            _closedPosition = transform.localPosition;
            _closedRotation = transform.localRotation;
        }

        // 0 = closed, 1 = fully open.
        public void SetProgress(float progress)
        {
            if (_motion == DoorMotion.Swing)
            {
                transform.localRotation = _closedRotation * Quaternion.Euler(0f, _amount * progress, 0f);
                return;
            }
            transform.localPosition = _closedPosition + SlideDirection * (_amount * progress);
        }

        private Vector3 SlideDirection => _motion switch
        {
            DoorMotion.SlideUp    => Vector3.up,
            DoorMotion.SlideDown  => Vector3.down,
            DoorMotion.SlideLeft  => Vector3.left,
            DoorMotion.SlideRight => Vector3.right,
            _                     => Vector3.zero,
        };

        public string GetInteractLabel(GameObject interactor) => _door.GetInteractLabel(interactor);
        public void   Interact(GameObject interactor)         => _door.Interact(interactor);
        public bool   CanInteract(GameObject interactor)      => _door.CanInteract(interactor);
        public float  HoldDuration                            => _door.HoldDuration;
        public float  HoldDurationFor(GameObject interactor)  => _door.HoldDurationFor(interactor);
        public InteractionStyle StyleFor(GameObject interactor) => _door.StyleFor(interactor);
    }
}
