using UnityEngine;
using CGD.Input;
using CGD.Interaction;

namespace CGD.Player
{
    // Picks the interactable the player is looking at most directly within range and
    // triggers it on the Interact key — instantly, or after holding the key for the
    // target's HoldDuration.
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float     _range           = 2.5f;
        [SerializeField] private LayerMask _interactMask    = ~0;
        [SerializeField] private Transform _forwardReference;   // assign camera transform

        [Header("Line of Sight")]
        [Tooltip("Ignore interactables with solid geometry between the view and them")]
        [SerializeField] private bool      _requireLineOfSight = true;
        [SerializeField] private LayerMask _occlusionMask      = ~0;

        public bool          HasTarget      => _current != null;
        public string        TargetLabel    => _current?.InteractLabel ?? string.Empty;
        public IInteractable Current        => _current;
        public Vector3       TargetPosition => _currentCollider != null ? _currentCollider.transform.position : Vector3.zero;
        // 0..1 while holding the key on a hold interaction; 0 otherwise.
        public float   HoldProgress   => _current != null && _current.HoldDuration > 0f
            ? Mathf.Clamp01(_holdTimer / _current.HoldDuration)
            : 0f;

        private PlayerInputHandler _input;
        private IInteractable      _current;
        private Collider           _currentCollider;
        private float              _holdTimer;
        private bool               _holdCompleted;

        private readonly Collider[]   _buffer    = new Collider[16];
        private readonly RaycastHit[] _losBuffer = new RaycastHit[8];

        private void Awake()
        {
            _input = GetComponent<PlayerInputHandler>();
        }

        private void Update()
        {
            IInteractable previous = _current;
            FindBest();
            if (_current != previous) ResetHold();

            if (_current == null) return;

            if (_current.HoldDuration <= 0f)
            {
                if (_input.GetAction(GameAction.Interact))
                    _current.Interact(gameObject);
                return;
            }

            TickHold();
        }

        private void TickHold()
        {
            if (!_input.IsHeld(GameAction.Interact))
            {
                ResetHold();
                return;
            }

            // One completion per press: the key must be released before holding again.
            if (_holdCompleted) return;

            _holdTimer += Time.deltaTime;
            if (_holdTimer < _current.HoldDuration) return;

            _holdCompleted = true;
            _holdTimer     = 0f;
            _current.Interact(gameObject);
        }

        private void ResetHold()
        {
            _holdTimer     = 0f;
            _holdCompleted = false;
        }

        private void FindBest()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _range, _buffer, _interactMask, QueryTriggerInteraction.Collide);

            Vector3 forward   = _forwardReference != null ? _forwardReference.forward : transform.forward;
            Vector3 eyeOrigin = _forwardReference != null ? _forwardReference.position : transform.position;

            IInteractable best         = null;
            Collider      bestCollider = null;
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

                // Score favours objects more aligned with look direction; distance is secondary.
                float score = alignment - mag / _range;
                if (score <= bestScore) continue;
                if (_requireLineOfSight && IsOccluded(eyeOrigin, dir, mag, col)) continue;

                bestScore    = score;
                best         = candidate;
                bestCollider = col;
            }

            _current         = best;
            _currentCollider = bestCollider;
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
