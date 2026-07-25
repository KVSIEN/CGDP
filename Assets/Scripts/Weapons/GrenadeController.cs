using System;
using UnityEngine;

// Hold the grenade action to aim (draws a predicted trajectory arc), release to
// throw. Independent of the ability/weapon slots — its own dedicated action and
// a limited carried count, like ammo.
[RequireComponent(typeof(PlayerInputHandler))]
public class GrenadeController : MonoBehaviour
{
    [SerializeField] private GrenadeData    _data;
    [SerializeField] private PlayerCamera   _camera;
    [SerializeField] private bool           _debugDrawTrajectory = true;

    /// <summary>Fired whenever the carried count changes.</summary>
    public event Action<int> OnCountChanged;

    public int Carried => _carried;

    private PlayerInputHandler _input;
    private PlayerMovement     _movement;
    private Collider           _ownerCollider;

    private int  _carried;
    private bool _heldLastFrame;

    private void Awake()
    {
        _input         = GetComponent<PlayerInputHandler>();
        _movement      = GetComponent<PlayerMovement>();
        _ownerCollider = GetComponent<Collider>();
        _carried       = _data != null ? _data.MaxCarried : 0;
    }

    private void Update()
    {
        if (_data == null) return;

        if (_movement != null && !_movement.CanAct)
        {
            _heldLastFrame = false;
            return;
        }

        bool held = _input.IsHeld(GameAction.Grenade) && _carried > 0;
        bool releasedThisFrame = _heldLastFrame && !held;
        _heldLastFrame = held;

        if (held && _debugDrawTrajectory)
            DrawTrajectoryPreview();

        if (releasedThisFrame)
            Throw();
    }

    /// <summary>Adds to the carried count — call from a grenade pickup.</summary>
    public void AddGrenades(int amount)
    {
        if (_data == null || amount <= 0) return;
        _carried = Mathf.Min(_carried + amount, _data.MaxCarried);
        OnCountChanged?.Invoke(_carried);
    }

    private Vector3 ThrowOrigin => _camera.transform.position + _camera.transform.forward * 0.5f;

    private void Throw()
    {
        if (_data.GrenadePrefab == null || _carried <= 0) return;

        var go = Instantiate(_data.GrenadePrefab, ThrowOrigin, Quaternion.identity);

        if (go.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = _camera.transform.forward * _data.ThrowForce;

        if (_ownerCollider != null && go.TryGetComponent<Collider>(out var col))
            Physics.IgnoreCollision(col, _ownerCollider);

        if (go.TryGetComponent<Grenade>(out var grenade))
            grenade.Init(_data);

        _carried--;
        OnCountChanged?.Invoke(_carried);

        _data.ThrowSound?.Play(transform.position);
    }

    private void DrawTrajectoryPreview()
    {
        Vector3 pos = ThrowOrigin;
        Vector3 vel = _camera.transform.forward * _data.ThrowForce;

        for (int i = 0; i < _data.TrajectorySteps; i++)
        {
            Vector3 next = pos + vel * _data.TrajectoryStepTime
                + 0.5f * Physics.gravity * (_data.TrajectoryStepTime * _data.TrajectoryStepTime);

            if (Physics.Linecast(pos, next, out RaycastHit hit, _data.HitMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawLine(pos, hit.point, _data.DebugColor);
                break;
            }

            Debug.DrawLine(pos, next, _data.DebugColor);
            vel += Physics.gravity * _data.TrajectoryStepTime;
            pos = next;
        }
    }
}
