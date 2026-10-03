using UnityEngine;

namespace CGD.Interaction
{
    // Attach to a door GameObject with a Collider, pivoted at the hinge edge (not
    // the center). Press E to swing it open/closed; a Switch can also call Toggle()
    // directly. No animation clips — the swing is a plain procedural rotation.
    //
    // With a Key set the door starts locked: interacting while carrying the key unlocks
    // and opens it (optionally using the key up), and it stays unlocked afterwards.
    // With a Condition set it also stays shut until that ConditionLock opens (terminals
    // switched on, signals raised) — "do A and B to open the door".
    //
    // A one-way door (MakeOneWay, set by the level builder) is barred from one side: it
    // only opens for someone standing on its open side, and works both ways after that —
    // a shortcut the player unbars from the far end.
    [RequireComponent(typeof(Collider))]
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _openAngle = 90f;
        [SerializeField] private float _openSpeed = 120f;
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField] private float _holdDuration = 0f;

        [Header("Lock")]
        [Tooltip("Item needed to unlock the door. Empty = never locked.")]
        [SerializeField] private ItemRequirement _key;
        [Tooltip("Optional: the door stays locked until this opens")]
        [SerializeField] private ConditionLock _condition;

        public float HoldDuration => _holdDuration;
        public bool  IsLocked     => _isLocked || IsHeldByCondition;

        private bool IsHeldByCondition => _condition != null && !_condition.Progress.IsOpen;

        private bool        _isOpen;
        private bool        _isLocked;
        private float       _currentAngle;
        private Quaternion  _closedRotation;
        private bool        _isBarred;
        private Vector3     _openSide;

        private void Awake()
        {
            _closedRotation = transform.localRotation;
            _isLocked       = !_key.IsNone;
        }

        private void Update()
        {
            float targetAngle = _isOpen ? _openAngle : 0f;
            if (Mathf.Approximately(_currentAngle, targetAngle)) return;

            _currentAngle = Mathf.MoveTowards(_currentAngle, targetAngle, _openSpeed * Time.deltaTime);
            transform.localRotation = _closedRotation * Quaternion.Euler(0f, _currentAngle, 0f);
        }

        public string GetInteractLabel(GameObject interactor)
        {
            if (_isBarred && !IsOnOpenSide(interactor)) return "Barred from the other side";
            if (IsHeldByCondition) return $"Locked  ({_condition.Describe()})";
            if (!_isLocked) return _isOpen ? "Close" : "Open";
            return _key.IsMetBy(interactor) ? "Unlock" : $"Locked  ({_key.Describe()})";
        }

        public void Interact(GameObject interactor)
        {
            if (_isBarred)
            {
                if (!IsOnOpenSide(interactor)) return;
                _isBarred = false;
            }

            if (IsHeldByCondition) return;

            if (_isLocked)
            {
                if (!_key.TryUse(interactor)) return;
                _isLocked = false;
            }

            Toggle();
        }

        // Remote control (switches, scripted events) bypasses both locks.
        public void Toggle() => _isOpen = !_isOpen;

        public void Unlock() => _isLocked = false;

        // Bars the door until opened by someone on the `openSide` (world direction from the door).
        public void MakeOneWay(Vector3 openSide)
        {
            _isBarred = true;
            _openSide = openSide;
        }

        private bool IsOnOpenSide(GameObject interactor) =>
            Vector3.Dot(interactor.transform.position - transform.position, _openSide) > 0f;
    }
}
