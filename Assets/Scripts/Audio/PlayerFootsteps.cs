using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerFootsteps : MonoBehaviour
{
    [SerializeField] private SurfaceDatabase _surfaces;

    [Header("Step Intervals")]
    [SerializeField] private float _walkInterval   = 0.5f;
    [SerializeField] private float _sprintInterval = 0.35f;
    [SerializeField] private float _crouchInterval = 0.7f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private float _rayDistance = 0.5f;

    private PlayerMovement _movement;
    private float _stepTimer;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!_movement.IsGrounded || _movement.IsSliding || _movement.IsMantling)
        {
            _stepTimer = 0f;
            return;
        }

        Vector3 hv = new Vector3(_movement.Velocity.x, 0f, _movement.Velocity.z);
        if (hv.sqrMagnitude < 0.5f)
        {
            _stepTimer = 0f;
            return;
        }

        float interval = _movement.IsSprinting  ? _sprintInterval
                        : _movement.IsCrouching ? _crouchInterval
                        : _walkInterval;

        _stepTimer += Time.deltaTime;
        if (_stepTimer < interval) return;
        _stepTimer -= interval;

        PlayFootstep();
    }

    private void PlayFootstep()
    {
        SoundBank bank = ResolveBank();
        bank?.Play(transform.position);
    }

    private SoundBank ResolveBank()
    {
        SoundBank walk, sprint, crouch;

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out var hit, _rayDistance, _groundMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent<SurfaceTag>(out var tag))
            {
                walk   = tag.Walk;
                sprint = tag.Sprint;
                crouch = tag.Crouch;
                return PickForState(walk, sprint, crouch);
            }

            if (_surfaces != null && hit.collider.sharedMaterial != null)
            {
                _surfaces.GetBanks(hit.collider.sharedMaterial, out walk, out sprint, out crouch);
                return PickForState(walk, sprint, crouch);
            }
        }

        if (_surfaces != null)
        {
            _surfaces.GetDefaults(out walk, out sprint, out crouch);
            return PickForState(walk, sprint, crouch);
        }

        return null;
    }

    private SoundBank PickForState(SoundBank walk, SoundBank sprint, SoundBank crouch)
    {
        if (_movement.IsSprinting)  return sprint ?? walk;
        if (_movement.IsCrouching)  return crouch ?? walk;
        return walk;
    }
}
