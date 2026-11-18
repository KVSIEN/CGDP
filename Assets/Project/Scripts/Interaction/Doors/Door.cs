using Unity.AI.Navigation;
using UnityEngine;

namespace CGD.Interaction
{
    // Press E to open/close; a Switch can also call Toggle() directly, and a DoorSensor can
    // open it automatically. No animation clips — every motion is procedural.
    //
    // Two ways to build one. Simple: attach it to a GameObject with a Collider, pivoted at the
    // hinge edge (not the center); it swings by Open Angle. Or give it DoorPanels (children
    // carrying the colliders): they swing or slide as the panels say — single, double, split,
    // vertical or sideways — and the door just says when they open.
    //
    // With a Key set the door starts locked: interacting while carrying the key unlocks
    // and opens it (optionally using the key up), and it stays unlocked afterwards.
    // With a Condition set it also stays shut until that ConditionLock opens (terminals
    // switched on, signals raised) — "do A and B to open the door".
    //
    // A one-way door (MakeOneWay, set by the level builder) is barred from one side: it
    // only opens for someone standing on its open side, and works both ways after that —
    // a shortcut the player unbars from the far end.
    public class Door : MonoBehaviour, IInteractable
    {
        [Tooltip("Seconds a full open or close takes")]
        [SerializeField, Min(0.05f)] private float _openDuration = 0.4f;
        [Tooltip("Swing of a door with no DoorPanels (it turns itself about its origin)")]
        [SerializeField] private float _openAngle = 90f;
        [Tooltip("Untick for a sensor-only door: no prompt to open it, but a key, condition or bar still shows one")]
        [SerializeField] private bool _interactable = true;
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField] private float _holdDuration = 0f;

        [Tooltip("Leave the door out of the NavMesh so enemies path straight through it, as through an open doorway. For sensor doors, which open for them on arrival; never for a locked one")]
        [SerializeField] private bool _agentsPathThrough;

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
        private float       _progress;
        private DoorPanel[] _panels;
        private Quaternion  _closedRotation;
        private bool        _isBarred;
        private Vector3     _openSide;

        private void Awake()
        {
            _panels         = GetComponentsInChildren<DoorPanel>(true);
            if (_agentsPathThrough) LeaveOutOfNavMesh();
            _closedRotation = transform.localRotation;
            _isLocked       = !_key.IsNone;
        }

        private void Update()
        {
            float target = _isOpen ? 1f : 0f;
            if (_progress == target) return;

            _progress = Mathf.MoveTowards(_progress, target, Time.deltaTime / _openDuration);
            if (_panels.Length == 0)
                transform.localRotation = _closedRotation * Quaternion.Euler(0f, _openAngle * _progress, 0f);
            else
                foreach (DoorPanel panel in _panels) panel.SetProgress(_progress);
        }

        // Added before the level's NavMesh is built (doors are placed first), so its closed
        // panels don't cut the walkable ground.
        private void LeaveOutOfNavMesh()
        {
            foreach (Collider body in GetComponentsInChildren<Collider>(true))
                body.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
        }

        public string GetInteractLabel(GameObject interactor)
        {
            if (_isBarred && !IsOnOpenSide(interactor)) return "Barred from the other side";
            if (IsHeldByCondition) return $"Locked  ({_condition.Describe()})";
            if (!_isLocked) return _isOpen ? "Close" : "Open";
            return _key.IsMetBy(interactor) ? "Unlock" : $"Locked  ({_key.Describe()})";
        }

        // A sensor-only door has no prompt, except while something still stands between you and it.
        public bool CanInteract(GameObject interactor) => _interactable || IsLocked || _isBarred;

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

        public bool IsOpen => _isOpen;

        // Automatic opening (a DoorSensor) never gets past a key, a condition or a bar.
        public bool CanOpenAutomatically => !IsLocked && !_isBarred;

        public void SetOpen(bool open) => _isOpen = open;

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
