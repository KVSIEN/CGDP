using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMantle : MonoBehaviour
{
    [SerializeField] private PlayerMovementSettings _settings;
    [SerializeField] private Transform _cameraTransform;

    [SerializeField] private bool _isMantling;

    public bool IsMantling => _isMantling;

    private PlayerMovement _movement;
    private Rigidbody _rb;
    private CapsuleCollider _col;

    private Vector3 _mantleTarget;
    private Vector3 _mantleLiftTarget;
    private Vector3 _mantleExitVelocity;
    private bool _mantleInLiftPhase;
    private float _mantleTimer;

    private void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _col      = GetComponent<CapsuleCollider>();
        _movement = GetComponent<PlayerMovement>();
    }

    public void Tick()
    {
        if (!_isMantling) return;

        _mantleTimer -= Time.fixedDeltaTime;

        Vector3 target = _mantleInLiftPhase ? _mantleLiftTarget : _mantleTarget;
        float dist     = Vector3.Distance(_rb.position, target);

        // Phase transition: lift complete → begin forward step
        if (_mantleInLiftPhase && dist < 0.08f)
        {
            _mantleInLiftPhase = false;
            return;
        }

        // Done: reached final target or timed out
        if (!_mantleInLiftPhase && (dist < 0.08f || _mantleTimer <= 0f))
        {
            _rb.position       = _mantleTarget;
            _rb.isKinematic    = false;
            _rb.linearVelocity = _mantleExitVelocity;
            _isMantling        = false;
            return;
        }

        // Lift slightly faster so it feels snappy upward
        float speed = _mantleInLiftPhase ? _settings.MantleSpeed * 1.5f : _settings.MantleSpeed;
        _rb.MovePosition(Vector3.MoveTowards(_rb.position, target, speed * Time.fixedDeltaTime));
    }

    public void TryInit()
    {
        Vector3 forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;

        // Sweep the full mantleable height range so the wall is detected whether
        // the player is at ground level or has already risen near the ledge top.
        Vector3 sweepBase = transform.position + Vector3.up * (_col.radius + 0.05f);
        Vector3 sweepTop  = transform.position + Vector3.up * _settings.MantleMaxHeight;
        if (!Physics.CapsuleCast(sweepBase, sweepTop, _col.radius * 0.4f, forward,
                out RaycastHit wallHit, _settings.MantleReach, _settings.GroundMask, QueryTriggerInteraction.Ignore))
            return;

        // Cast down from above the wall hit point to find the ledge top surface.
        float castTopY = transform.position.y + _settings.MantleMaxHeight + 0.2f;
        Vector3 castFrom = new Vector3(
            wallHit.point.x + forward.x * 0.05f,
            castTopY,
            wallHit.point.z + forward.z * 0.05f);

        if (!Physics.Raycast(castFrom, Vector3.down, out RaycastHit ledgeHit,
                _settings.MantleMaxHeight + 0.5f, _settings.GroundMask, QueryTriggerInteraction.Ignore))
            return;

        float relativeHeight = ledgeHit.point.y - transform.position.y;
        if (relativeHeight < 0f) return;

        if (relativeHeight <= _settings.VaultMaxHeight)
        {
            // Low vault: kinematically lift to the ledge top to avoid clipping the edge,
            // then release with forward + upward velocity so the player arcs over.
            _mantleLiftTarget   = new Vector3(_rb.position.x, ledgeHit.point.y, _rb.position.z);
            _mantleTarget       = _mantleLiftTarget;
            _mantleInLiftPhase  = false;
            _mantleExitVelocity = new Vector3(
                forward.x * _settings.VaultForwardImpulse,
                _settings.VaultUpImpulse,
                forward.z * _settings.VaultForwardImpulse);
            _isMantling        = true;
            _mantleTimer       = 0.3f;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic    = true;
            return;
        }

        if (relativeHeight <= _settings.MantleMaxHeight)
        {
            // High mantle: phase 1 — lift straight up alongside the wall to ledge height
            //              phase 2 — step forward onto the ledge surface.
            // Kinematic during the move prevents physics from pushing the player back
            // through the wall face.
            _mantleLiftTarget  = new Vector3(_rb.position.x, ledgeHit.point.y, _rb.position.z);
            _mantleTarget = new Vector3(
                wallHit.point.x + forward.x * _settings.MantleStepOver,
                ledgeHit.point.y,
                wallHit.point.z + forward.z * _settings.MantleStepOver);
            _mantleInLiftPhase  = true;
            _mantleExitVelocity = Vector3.zero;
            _isMantling        = true;
            _mantleTimer       = _settings.MantleTimeout;
            _rb.linearVelocity = Vector3.zero;
            _rb.isKinematic    = true;
        }
    }
}
