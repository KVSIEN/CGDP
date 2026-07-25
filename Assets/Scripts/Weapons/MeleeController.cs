using UnityEngine;

// Standalone light/heavy melee attack, independent of the equipped ranged weapon.
// Tap the melee action for a light attack (chains into the next combo step if
// pressed again during Active/Recovery); hold past HeavyHoldThreshold before
// releasing for a heavy finisher instead.
[RequireComponent(typeof(PlayerInputHandler))]
public class MeleeController : MonoBehaviour
{
    private enum Phase { Idle, Windup, Active, Recovery }

    [SerializeField] private MeleeWeaponData _data;
    [SerializeField] private PlayerCamera    _camera;

    [Header("Debug")]
    [SerializeField] private bool  _debugDrawHitbox   = true;
    [SerializeField] private float _debugDrawDuration = 0.5f;

    private static readonly Collider[] _hitBuffer = new Collider[16];

    private PlayerInputHandler _input;
    private PlayerMovement     _movement;

    private Phase _phase;
    private float _phaseTimer;
    private int   _comboIndex;
    private float _comboResetTimer;
    private MeleeAttackStep _activeStep;

    private bool  _heldLastFrame;
    private float _holdTimer;
    private bool  _comboBuffered;
    private bool  _bufferedHeavy;

    private void Awake()
    {
        _input    = GetComponent<PlayerInputHandler>();
        _movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (_data == null) return;
        if (_movement != null && !_movement.CanAct) return;

        bool held = _input.IsHeld(GameAction.Melee);
        bool releasedThisFrame = _heldLastFrame && !held;
        _heldLastFrame = held;
        if (held) _holdTimer += Time.deltaTime;

        if (_phase == Phase.Idle)
        {
            if (_comboResetTimer > 0f)
            {
                _comboResetTimer -= Time.deltaTime;
                if (_comboResetTimer <= 0f) _comboIndex = 0;
            }

            if (releasedThisFrame)
            {
                StartAttack(_holdTimer >= _data.HeavyHoldThreshold);
                _holdTimer = 0f;
            }
            return;
        }

        if (releasedThisFrame)
        {
            if (!_comboBuffered)
            {
                _comboBuffered = true;
                _bufferedHeavy = _holdTimer >= _data.HeavyHoldThreshold;
            }
            _holdTimer = 0f;
        }

        TickPhase(Time.deltaTime);
    }

    private void StartAttack(bool heavy)
    {
        _activeStep = heavy ? _data.HeavyAttack : _data.LightCombo[_comboIndex];
        _comboIndex = heavy ? 0 : (_comboIndex + 1) % _data.LightCombo.Length;

        _phase      = Phase.Windup;
        _phaseTimer = _activeStep.WindupTime;

        _activeStep.SwingSound?.Play(transform.position);
    }

    private void TickPhase(float dt)
    {
        _phaseTimer -= dt;
        if (_phaseTimer > 0f) return;

        switch (_phase)
        {
            case Phase.Windup:
                _phase      = Phase.Active;
                _phaseTimer = _activeStep.ActiveTime;
                ResolveHit();
                break;

            case Phase.Active:
                _phase      = Phase.Recovery;
                _phaseTimer = _activeStep.RecoveryTime;
                break;

            case Phase.Recovery:
                EndAttack();
                break;
        }
    }

    private void EndAttack()
    {
        if (_comboBuffered)
        {
            _comboBuffered = false;
            StartAttack(_bufferedHeavy);
            return;
        }

        _phase           = Phase.Idle;
        _comboResetTimer = _data.ComboResetTime;
    }

    private void ResolveHit()
    {
        Vector3 origin  = _camera.transform.position;
        Vector3 forward = _camera.transform.forward;
        Vector3 center  = origin + forward * _activeStep.Range;

        int count = Physics.OverlapSphereNonAlloc(center, _activeStep.Radius, _hitBuffer,
            _data.HitMask, QueryTriggerInteraction.Ignore);

        var info = new DamageInfo(_activeStep.Damage, _activeStep.ArmorPenetration, _activeStep.DamageType);

        bool hitAnything = false;
        for (int i = 0; i < count; i++)
        {
            if (_hitBuffer[i].transform.root == transform.root) continue;
            if (_hitBuffer[i].TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(info);
                hitAnything = true;
            }
        }

        if (hitAnything)
            _activeStep.HitSound?.Play(center);

        if (_debugDrawHitbox)
            DebugDrawSphere(center, _activeStep.Radius, _activeStep.DebugColor, _debugDrawDuration);
    }

    // No animations yet — draw a wireframe sphere so the attack's timing and
    // reach are visible in the Scene view while playing.
    private static void DebugDrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        DrawCircle(center, Vector3.right, Vector3.up,      radius, color, duration);
        DrawCircle(center, Vector3.up,    Vector3.forward, radius, color, duration);
        DrawCircle(center, Vector3.forward, Vector3.right, radius, color, duration);
    }

    private static void DrawCircle(Vector3 center, Vector3 tangent, Vector3 bitangent, float radius, Color color, float duration)
    {
        const int segments = 16;
        Vector3 prev = center + tangent * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = center + (tangent * Mathf.Cos(t) + bitangent * Mathf.Sin(t)) * radius;
            Debug.DrawLine(prev, next, color, duration);
            prev = next;
        }
    }
}
