using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Input;
using CGD.Interaction;

namespace CGD.Player
{
    // Picks the interactable the player is looking at most directly within range (higher
    // InteractPriority first) and triggers it on the Interact key — instantly, or after
    // holding the key for the target's HoldDuration or tapping it repeatedly (mashing). Focus listeners on the target are
    // told when it gains and loses focus, for highlights and similar feedback.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float     _range           = 2.5f;
        [SerializeField] private LayerMask _interactMask    = ~0;
        [SerializeField] private Transform _forwardReference;   // assign camera transform

        [Header("Line of Sight")]
        [Tooltip("Ignore interactables with solid geometry between the view and them. Pickups are exempt.")]
        [SerializeField] private bool      _requireLineOfSight = true;
        [SerializeField] private LayerMask _occlusionMask      = ~0;

        [Header("View")]
        [Tooltip("Ignore interactables outside the camera's view frustum")]
        [SerializeField] private bool      _requireOnScreen    = true;

        public bool          HasTarget      => _current != null;
        public string        TargetLabel    => _current != null ? _current.GetInteractLabel(gameObject) : string.Empty;
        public IInteractable Current        => _current;
        public Vector3       TargetPosition => _currentCollider != null ? _currentCollider.transform.position : Vector3.zero;
        // How the target is used, so the prompt can say so.
        public InteractionStyle TargetStyle => _current != null ? _current.Style : InteractionStyle.Press;

        // 0..1 while holding or mashing toward finishing the target; 0 for a plain press.
        public float   EffortProgress => TargetStyle != InteractionStyle.Press ? _effort.Progress : 0f;

        // Counts every tap made on a mash target, for feedback that reacts to each one.
        public int     TapCount       { get; private set; }

        // The target changed, or the current one was used (its label may have changed,
        // e.g. a door that now opens instead of closes). Lets UI rebuild the label only then.
        public event Action TargetChanged;

        private PlayerInputHandler _input;
        private PlayerRestraint    _restraint;
        private IInteractable      _current;
        private Collider           _currentCollider;
        private readonly EffortMeter _effort = new();
        private Camera             _camera;
        private readonly Plane[]   _frustum = new Plane[6];

        private readonly Collider[]   _buffer    = new Collider[16];
        private readonly RaycastHit[] _losBuffer = new RaycastHit[8];
        private readonly List<IInteractionFocusListener> _focusListeners = new();

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
            TryGetComponent(out _restraint);
            if (_forwardReference == null || !_forwardReference.TryGetComponent(out _camera))
                _camera = GetComponentInChildren<Camera>(true);
        }

        private void Update()
        {
            IInteractable previous         = _current;
            Collider      previousCollider = _currentCollider;

            // Held in place: nothing to interact with, and Interact is for breaking free.
            if (_restraint != null && _restraint.IsRestrained) ClearTarget();
            else FindBest();
            if (_current != previous)
            {
                ResetHold();
                NotifyFocus(previousCollider, false);
                NotifyFocus(_currentCollider, true);
                TargetChanged?.Invoke();
            }

            if (_current == null) return;

            switch (_current.Style)
            {
                case InteractionStyle.Hold:
                    // One completion per press: the key must be released before holding again.
                    if (_effort.Hold(_input.IsHeld(GameAction.Interact), _current.HoldDuration, Time.deltaTime))
                        Use();
                    break;

                case InteractionStyle.Mash:
                    TickMash();
                    break;

                default:
                    if (_input.GetAction(GameAction.Interact))
                        Use();
                    break;
            }
        }

        private void TickMash()
        {
            _effort.Drain(_current.TapDecayPerSecond, Time.deltaTime);
            if (!_input.WasPressed(GameAction.Interact)) return;

            TapCount++;
            if (_effort.Tap(_current.TapsRequired)) Use();
        }

        private void Use()
        {
            _current.Interact(gameObject);
            TargetChanged?.Invoke();
        }

        private void ResetHold() => _effort.Reset();

        private void FindBest()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _range, _buffer, _interactMask, QueryTriggerInteraction.Collide);

            Vector3 forward   = _forwardReference != null ? _forwardReference.forward : transform.forward;
            Vector3 eyeOrigin = _forwardReference != null ? _forwardReference.position : transform.position;

            bool checkView = _requireOnScreen && _camera != null;
            if (checkView) GeometryUtility.CalculateFrustumPlanes(_camera, _frustum);

            IInteractable best         = null;
            Collider      bestCollider = null;
            int           bestPriority = int.MinValue;
            float         bestScore    = float.MinValue;

            for (int i = 0; i < count; i++)
            {
                Collider col = _buffer[i];
                if (!col.TryGetComponent<IInteractable>(out var candidate)) continue;
                if (!candidate.CanInteract(gameObject)) continue;

                Vector3 toTarget  = col.bounds.center - eyeOrigin;
                float   mag       = toTarget.magnitude;
                Vector3 dir       = mag > 0.05f ? toTarget / mag : forward;
                float   alignment = Vector3.Dot(forward, dir);
                if (alignment <= 0f) continue;
                if (checkView && !GeometryUtility.TestPlanesAABB(_frustum, col.bounds)) continue;

                // Priority decides first; then the score favours objects more aligned with
                // the look direction, with distance secondary.
                int   priority = candidate.InteractPriority;
                float score    = alignment - mag / _range;
                if (priority < bestPriority || (priority == bestPriority && score <= bestScore)) continue;
                if (_requireLineOfSight && !candidate.IgnoreLineOfSight && IsOccluded(eyeOrigin, dir, mag, col)) continue;

                bestPriority = priority;
                bestScore    = score;
                best         = candidate;
                bestCollider = col;
            }

            _current         = best;
            _currentCollider = bestCollider;
        }

        private void ClearTarget()
        {
            _current         = null;
            _currentCollider = null;
        }

        private void OnDisable()
        {
            NotifyFocus(_currentCollider, false);
            _current         = null;
            _currentCollider = null;
            ResetHold();
        }

        private void NotifyFocus(Collider target, bool focused)
        {
            if (target == null) return;

            target.GetComponents(_focusListeners);
            foreach (IInteractionFocusListener listener in _focusListeners)
                listener.OnInteractionFocus(focused);
            _focusListeners.Clear();
        }

        // Blocked when a solid collider other than the target itself or the player's own
        // colliders lies between the view and the target.
        private bool IsOccluded(Vector3 origin, Vector3 dir, float distance, Collider target)
        {
            int hits = Physics.RaycastNonAlloc(origin, dir, _losBuffer, distance, _occlusionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits; i++)
            {
                Collider hit = _losBuffer[i].collider;
                if (hit == target || hit.transform.root == transform.root) continue;
                if (hit.transform.IsChildOf(target.transform)) continue;
                return true;
            }
            return false;
        }
    }
}
