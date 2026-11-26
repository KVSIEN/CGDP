using Unity.AI.Navigation;
using UnityEngine;
using CGD.Combat;
using CGD.Stealth;

namespace CGD.Interaction
{
    // A door, opened one of two ways. No animation clips — every motion is procedural.
    //
    // A handle door (Button Operated off): press E on the door itself to open or close it; it
    // stays as you leave it.
    // A button door (Button Operated on): press a DoorButton beside the frame and it opens, runs
    // its motor (noise) and closes by itself after Stay Open For. Crouched at the door itself
    // you can instead hold E to force it a gap open — slower, but silent — which slides shut
    // again once nobody is in the doorway. A DoorSensor can open it for enemies.
    //
    // A door never closes on someone: it waits while anyone stands in its doorway.
    // A Switch can call Toggle() directly.
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
        // How often the doorway is checked for someone in the way, while it matters.
        private const float DoorwayPollInterval = 0.1f;
        // Seconds a forced gap waits for its forcer to step into the doorway, and lingers after.
        private const float ForcedGapGrace  = 3f;
        private const float ForcedGapLinger = 0.5f;
        private const int   DoorwayBufferSize = 16;

        [Tooltip("Seconds a full open or close takes")]
        [SerializeField, Min(0.05f)] private float _openDuration = 0.4f;
        [Tooltip("Swing of a door with no DoorPanels (it turns itself about its origin)")]
        [SerializeField] private float _openAngle = 90f;
        [Tooltip("Seconds the Interact key must be held (0 = instant)")]
        [SerializeField] private float _holdDuration = 0f;

        [Header("Button door")]
        [Tooltip("Opened by its DoorButtons, closing by itself. Off = a handle door, used on the door itself and left as you leave it")]
        [SerializeField] private bool _buttonOperated;
        [Tooltip("Seconds a button door stays open before closing by itself (it waits while the doorway is occupied). 0 = until a button closes it")]
        [SerializeField, Min(0f)] private float _stayOpenFor = 3f;
        [Tooltip("Seconds of holding Interact, crouched at the door, to force it open silently. 0 = can't be forced")]
        [SerializeField, Min(0f)] private float _forceDuration = 2.5f;
        [Tooltip("How far it gives when forced: share of a full opening")]
        [SerializeField, Range(0f, 1f)] private float _squeezeOpening = 0.35f;

        [Header("Doorway")]
        [Tooltip("Centre of the doorway checked for someone in the way, in the door's space (its closed pose)")]
        [SerializeField] private Vector3 _doorwayCenter = new(0f, 1.1f, 0f);
        [Tooltip("Full size of that box: as wide as the opening, a step deep each side")]
        [SerializeField] private Vector3 _doorwaySize = new(3f, 2.2f, 2f);
        [SerializeField] private LayerMask _doorwayMask = ~0;

        [Tooltip("Leave the door out of the NavMesh so enemies path straight through it, as through an open doorway. For sensor doors, which open for them on arrival; never for a locked one")]
        [SerializeField] private bool _agentsPathThrough;

        [Header("Noise")]
        [Tooltip("How far the player opening it can be heard (0 = silent). A property of the door, not of how you move: a button door runs a motor and is loud, a handle door nearly silent. Forcing one is always silent")]
        [SerializeField, Min(0f)] private float _noiseRadius = 6f;
        [Tooltip("Closing it after the player, as a share of that")]
        [SerializeField, Range(0f, 1f)] private float _closeNoiseShare = 0.5f;

        [Header("Lock")]
        [Tooltip("Item needed to unlock the door. Empty = never locked.")]
        [SerializeField] private ItemRequirement _key;
        [Tooltip("Optional: the door stays locked until this opens")]
        [SerializeField] private ConditionLock _condition;

        public float HoldDuration => _holdDuration;
        public bool  IsLocked     => _isLocked || IsHeldByCondition;
        public bool  IsOpen       => _isOpen;
        public bool  IsButtonOperated => _buttonOperated;

        // Automatic opening (a DoorSensor) never gets past a key, a condition or a bar.
        public bool CanOpenAutomatically => !IsLocked && !_isBarred;

        private bool IsHeldByCondition => _condition != null && !_condition.Progress.IsOpen;

        private readonly Collider[] _doorwayBuffer = new Collider[DoorwayBufferSize];

        private bool        _isOpen;
        private bool        _isLocked;
        private float       _progress;
        private DoorPanel[] _panels;
        private Vector3     _closedPosition;
        private Quaternion  _closedRotation;
        private bool        _isBarred;
        private Vector3     _openSide;

        // A close asked for (by a button, the timer or a sensor), carried out once the doorway is clear.
        private bool       _closePending;
        private GameObject _closedBy;
        // When a button door closes by itself, and who opened it (the close is theirs to be heard).
        private float      _closeAt = -1f;
        private GameObject _openedBy;
        // A forced gap, held while someone is in the doorway.
        private bool  _squeezed;
        private float _releaseAt;
        private float _nextDoorwayPoll;
        private bool  _doorwayOccupied;

        // The last interactor asked about, and whether it can sneak (crouch): asked every frame.
        private GameObject  _asker;
        private Stealthable _askerStealth;

        private void Awake()
        {
            _panels         = GetComponentsInChildren<DoorPanel>(true);
            if (_agentsPathThrough) LeaveOutOfNavMesh();
            _closedPosition = transform.localPosition;
            _closedRotation = transform.localRotation;
            _isLocked       = !_key.IsNone;
        }

        private void Update()
        {
            if (_closePending || _closeAt >= 0f || _squeezed) TickDoorway();
            Animate();
        }

        private void TickDoorway()
        {
            if (Time.time >= _nextDoorwayPoll)
            {
                _nextDoorwayPoll = Time.time + DoorwayPollInterval;
                _doorwayOccupied = IsDoorwayOccupied();
            }

            if (_closeAt >= 0f && Time.time >= _closeAt)
            {
                _closeAt = -1f;
                RequestClose(_openedBy);
            }

            if (_closePending && !_doorwayOccupied)
            {
                _closePending = false;
                Change(false, _closedBy);
            }

            if (_squeezed)
            {
                if (_doorwayOccupied) _releaseAt = Mathf.Max(_releaseAt, Time.time + ForcedGapLinger);
                else if (Time.time >= _releaseAt) _squeezed = false;
            }
        }

        private void Animate()
        {
            float target = _isOpen ? 1f : _squeezed ? _squeezeOpening : 0f;
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

        // ── Used on the door itself (its leaves) ─────────────────────────────────────────
        // A handle door opens and closes here. A button door only shows what bars it, or, to
        // someone crouched, the way to force it.

        public string GetInteractLabel(GameObject interactor)
        {
            if (TryBlockedLabel(interactor, out string blocked)) return blocked;
            if (CanForce(interactor)) return "Force open  (silent)";
            return _isOpen ? "Close" : "Open";
        }

        public bool CanInteract(GameObject interactor) =>
            !_buttonOperated || IsBlocked(interactor) || CanForce(interactor);

        public float HoldDurationFor(GameObject interactor) => CanForce(interactor) ? _forceDuration : _holdDuration;

        public InteractionStyle StyleFor(GameObject interactor) =>
            HoldDurationFor(interactor) > 0f ? InteractionStyle.Hold : InteractionStyle.Press;

        public void Interact(GameObject interactor)
        {
            if (CanForce(interactor))
            {
                Force();
                return;
            }
            if (!TryGetPast(interactor)) return;
            if (_buttonOperated) Press(interactor);
            else Change(!_isOpen, interactor);
        }

        // ── Used through a DoorButton ────────────────────────────────────────────────────

        public string GetButtonLabel(GameObject interactor) =>
            TryBlockedLabel(interactor, out string blocked) ? blocked : _isOpen ? "Close" : "Open";

        public void PressButton(GameObject interactor)
        {
            if (!TryGetPast(interactor)) return;
            Press(interactor);
        }

        private void Press(GameObject interactor)
        {
            if (_isOpen)
            {
                _closeAt = -1f;
                RequestClose(interactor);
                return;
            }

            _squeezed = false;
            _openedBy = interactor;
            _closeAt  = _stayOpenFor > 0f ? Time.time + _stayOpenFor : -1f;
            Change(true, interactor);
        }

        // Pried a gap open by hand: no motor, no sound, no timer — it shuts once the doorway empties.
        private void Force()
        {
            _squeezed  = true;
            _releaseAt = Time.time + ForcedGapGrace;
        }

        private bool CanForce(GameObject interactor) =>
            _buttonOperated && _forceDuration > 0f && _squeezeOpening > 0f
            && !_isOpen && !_squeezed && CanOpenAutomatically && IsCrouched(interactor);

        // ── Locks and bars ───────────────────────────────────────────────────────────────

        private bool IsBlocked(GameObject interactor) => TryBlockedLabel(interactor, out _);

        private bool TryBlockedLabel(GameObject interactor, out string label)
        {
            label = null;
            if (_isBarred && !IsOnOpenSide(interactor)) label = "Barred from the other side";
            else if (IsHeldByCondition) label = $"Locked  ({_condition.Describe()})";
            else if (_isLocked) label = _key.IsMetBy(interactor) ? "Unlock" : $"Locked  ({_key.Describe()})";
            return label != null;
        }

        // Unbars or unlocks it for `interactor` where they may; false while something still holds it.
        private bool TryGetPast(GameObject interactor)
        {
            if (_isBarred)
            {
                if (!IsOnOpenSide(interactor)) return false;
                _isBarred = false;
            }

            if (IsHeldByCondition) return false;

            if (_isLocked)
            {
                if (!_key.TryUse(interactor)) return false;
                _isLocked = false;
            }
            return true;
        }

        // ── Remote and automatic control ─────────────────────────────────────────────────

        // Remote control (switches, scripted events) bypasses both locks.
        public void Toggle()
        {
            if (_isOpen) RequestClose(null);
            else Change(true, null);
        }

        // A sensor's call. Opened or shut by the player, a door makes its own noise; for anyone
        // else it is silent, or guards walking their rounds would keep alerting each other.
        public void SetOpen(bool open, GameObject by = null)
        {
            if (open)
            {
                _closePending = false;
                Change(true, by);
            }
            else if (_closeAt < 0f) RequestClose(by);   // a button's timer still running keeps it open
        }

        private void RequestClose(GameObject by)
        {
            if (!_isOpen) return;
            _closePending = true;
            _closedBy     = by;
        }

        private void Change(bool open, GameObject by)
        {
            if (open == _isOpen) return;
            _isOpen = open;
            if (!open) _closeAt = -1f;
            if (by == null || _noiseRadius <= 0f) return;

            DamageSource source = DamageSource.Of(by);
            if (source.Team != Team.Player) return;

            // Closing behind you is quieter than opening it.
            Noise.Emit(transform.position, open ? _noiseRadius : _noiseRadius * _closeNoiseShare, source);
        }

        public void Unlock() => _isLocked = false;

        // Bars the door until opened by someone on the `openSide` (world direction from the door).
        public void MakeOneWay(Vector3 openSide)
        {
            _isBarred = true;
            _openSide = openSide;
        }

        private bool IsOnOpenSide(GameObject interactor) =>
            Vector3.Dot(interactor.transform.position - transform.position, _openSide) > 0f;

        // ── Helpers ──────────────────────────────────────────────────────────────────────

        // Measured from the closed pose, so a door with no panels (which turns itself) keeps its box.
        private bool IsDoorwayOccupied()
        {
            Transform parent    = transform.parent;
            Quaternion rotation = parent != null ? parent.rotation * _closedRotation : _closedRotation;
            Vector3 origin      = parent != null ? parent.TransformPoint(_closedPosition) : _closedPosition;
            Vector3 center      = origin + rotation * _doorwayCenter;

            int count = Physics.OverlapBoxNonAlloc(center, _doorwaySize * 0.5f, _doorwayBuffer, rotation, _doorwayMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                HealthManager creature = _doorwayBuffer[i].GetComponentInParent<HealthManager>();
                if (creature != null && !creature.IsDead) return true;
            }
            return false;
        }

        private bool IsCrouched(GameObject interactor)
        {
            if (interactor == null) return false;
            // Looked up again until found: the stealth component may register after the first ask.
            if (interactor != _asker || _askerStealth == null)
            {
                _asker = interactor;
                HealthManager health = interactor.GetComponentInParent<HealthManager>();
                _askerStealth = health != null && Stealthable.TryGet(health, out Stealthable stealth) ? stealth : null;
            }
            return _askerStealth != null && _askerStealth.IsSneaking;
        }
    }
}
