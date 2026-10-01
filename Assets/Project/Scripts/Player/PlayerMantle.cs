using UnityEngine;

namespace CGD.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    // Runs before PlayerMovement's default execution order so IsMantling reflects
    // this frame's transition before PlayerMovement checks it in the same FixedUpdate.
    [DefaultExecutionOrder(-100)]
    public class PlayerMantle : MonoBehaviour
    {
        private const float ArriveDistance = 0.08f;
        // Lifting a bit faster than stepping forward makes the pull-up feel snappy.
        private const float LiftSpeedMultiplier = 1.5f;
        // Slack on top of the expected duration before a blocked mantle is abandoned.
        private const float TimeoutMultiplier = 2f;

        [SerializeField] private PlayerMovementSettings _settings;
        [SerializeField] private Transform _cameraTransform;

        [SerializeField] private bool _isMantling;

        public bool IsMantling => _isMantling;

        private Rigidbody _rb;
        private CapsuleCollider _col;

        private Vector3 _liftTarget;
        private Vector3 _target;
        private bool _inLiftPhase;
        private float _speed;
        private float _timer;

        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();
        }

        private void FixedUpdate()
        {
            if (!_isMantling) return;

            _timer -= Time.fixedDeltaTime;

            Vector3 target = _inLiftPhase ? _liftTarget : _target;
            float dist     = Vector3.Distance(_rb.position, target);

            if (_inLiftPhase && dist < ArriveDistance)
            {
                _inLiftPhase = false;
                return;
            }

            if ((!_inLiftPhase && dist < ArriveDistance) || _timer <= 0f)
            {
                Finish();
                return;
            }

            float speed = _inLiftPhase ? _speed * LiftSpeedMultiplier : _speed;
            _rb.MovePosition(Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime));
        }

        // Starts a mantle if there's a ledge within the mantleable height range in front
        // of the camera with room to stand on top. Returns whether a mantle started.
        public bool TryStart()
        {
            if (_isMantling) return false;

            Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
            if (!TryFindLedge(forward, out Vector3 wallPoint, out float ledgeY)) return false;

            float height = ledgeY - _rb.position.y;
            if (height < _settings.MantleMinHeight || height > _settings.MantleMaxHeight) return false;

            Vector3 target = new Vector3(
                wallPoint.x + forward.x * _settings.MantleStepOver,
                ledgeY,
                wallPoint.z + forward.z * _settings.MantleStepOver);
            if (!HasStandingRoom(target)) return false;

            Begin(target, height);
            return true;
        }

        private bool TryFindLedge(Vector3 forward, out Vector3 wallPoint, out float ledgeY)
        {
            wallPoint = default;
            ledgeY    = 0f;

            // Sweep the full mantleable height range so the wall is detected whether
            // the player is at ground level or has already risen near the ledge top.
            Vector3 origin    = _rb.position;
            Vector3 sweepBase = origin + Vector3.up * (_col.radius + 0.05f);
            Vector3 sweepTop  = origin + Vector3.up * _settings.MantleMaxHeight;
            if (!Physics.CapsuleCast(sweepBase, sweepTop, _col.radius * 0.4f, forward,
                    out RaycastHit wallHit, _settings.MantleReach, _settings.GroundMask, QueryTriggerInteraction.Ignore))
                return false;

            // Cast down from just above the max height, slightly past the wall face, to find the ledge top.
            Vector3 castFrom = new Vector3(
                wallHit.point.x + forward.x * 0.05f,
                origin.y + _settings.MantleMaxHeight + 0.2f,
                wallHit.point.z + forward.z * 0.05f);
            if (!Physics.Raycast(castFrom, Vector3.down, out RaycastHit ledgeHit,
                    _settings.MantleMaxHeight + 0.5f, _settings.GroundMask, QueryTriggerInteraction.Ignore))
                return false;

            wallPoint = wallHit.point;
            ledgeY    = ledgeHit.point.y;
            return true;
        }

        private bool HasStandingRoom(Vector3 feet)
        {
            float radius = _col.radius * 0.95f;
            Vector3 bottom = feet + Vector3.up * (radius + 0.05f);
            Vector3 top    = feet + Vector3.up * Mathf.Max(_col.height - radius, radius + 0.05f);
            return !Physics.CheckCapsule(bottom, top, radius, _settings.GroundMask, QueryTriggerInteraction.Ignore);
        }

        // Phase 1 lifts straight up alongside the wall to ledge height, phase 2 steps forward
        // onto the ledge. Kinematic during the move so physics can't push the player back
        // through the wall face.
        private void Begin(Vector3 target, float height)
        {
            _liftTarget  = new Vector3(_rb.position.x, target.y, _rb.position.z);
            _target      = target;
            _inLiftPhase = true;
            _speed       = GetSpeed(height);

            float liftDistance = Vector3.Distance(_rb.position, _liftTarget);
            float stepDistance = Vector3.Distance(_liftTarget, _target);
            float expectedTime = liftDistance / (_speed * LiftSpeedMultiplier) + stepDistance / _speed;
            _timer = expectedTime * TimeoutMultiplier;

            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic    = true;
            _isMantling        = true;
        }

        private void Finish()
        {
            _rb.position       = _target;
            _rb.isKinematic    = false;
            _rb.linearVelocity = Vector3.zero;
            _isMantling        = false;
        }

        private float GetSpeed(float height)
        {
            float t = Mathf.InverseLerp(_settings.MantleMinHeight, _settings.MantleMaxHeight, height);
            return Mathf.Max(0.01f, _settings.MantleSpeed * _settings.MantleSpeedByHeight.Evaluate(t));
        }
    }
}
