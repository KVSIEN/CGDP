using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Input;
using CGD.Player;

namespace CGD.Weapons
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class MeleeController : MonoBehaviour
    {
        private enum Phase { Idle, Windup, Active, Recovery }

        [SerializeField] private MeleeWeaponData _data;
        [SerializeField] private PlayerCamera    _camera;

        [Header("Debug")]
        [SerializeField] private bool  _debugDraw     = true;
        [SerializeField] private float _debugDuration = 0.3f;

        private readonly MeleeHitResolver _resolver = new();
        private readonly ActionTimelineRunner _runner = new();

        private PlayerInputHandler _input;
        private PlayerMovement     _movement;
        private DamageSource       _damageSource;

        private Phase _phase;
        private float _phaseTimer;
        private int   _comboIndex;
        private float _comboResetTimer;
        private MeleeAttackStep _activeStep;
        private bool _usingTimeline;
        private ActionContext _timelineCtx;

        private bool  _heldLastFrame;
        private float _holdTimer;
        private bool  _comboBuffered;
        private bool  _bufferedHeavy;

        private void Awake()
        {
            _input        = GetComponent<PlayerInputHandler>();
            _movement     = GetComponent<PlayerMovement>();
            _damageSource = DamageSource.Of(gameObject);
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

        private void FixedUpdate()
        {
            if (_phase != Phase.Active || _activeStep == null) return;

            Transform cam = _camera.transform;

            if (_usingTimeline)
            {
                _timelineCtx.Origin  = cam.position;
                _timelineCtx.Forward = cam.forward;
                _timelineCtx.Up      = cam.up;

                _runner.Tick();

                if (!_runner.IsRunning)
                    ExitActive();
            }
            else
            {
                _resolver.Tick(cam.position, cam.forward, cam.up, _debugDraw, _debugDuration);
            }
        }

        private void StartAttack(bool heavy)
        {
            _activeStep = heavy ? _data.HeavyAttack : _data.LightCombo[_comboIndex];
            _comboIndex = heavy ? 0 : (_comboIndex + 1) % _data.LightCombo.Length;

            _phase      = Phase.Windup;
            _phaseTimer = _activeStep.WindupTime;

            _activeStep.SwingSound.TryPlay(transform.position);
            Noise.Emit(transform.position, _data.NoiseRadius, _damageSource);
        }

        private void TickPhase(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer > 0f) return;

            switch (_phase)
            {
                case Phase.Windup:
                    EnterActive();
                    break;

                case Phase.Active:
                    if (!_usingTimeline)
                        ExitActive();
                    break;

                case Phase.Recovery:
                    EndAttack();
                    break;
            }
        }

        private void EnterActive()
        {
            _phase = Phase.Active;
            _usingTimeline = _activeStep.Timeline != null;

            if (_usingTimeline)
            {
                ActionTimeline timeline = _activeStep.Timeline;
                _phaseTimer = timeline.TotalFrames * Time.fixedDeltaTime;

                Transform cam = _camera.transform;
                _timelineCtx = new ActionContext
                {
                    Origin        = cam.position,
                    Forward       = cam.forward,
                    Up            = cam.up,
                    SourceRoot    = transform.root,
                    Source        = _damageSource,
                    HitMask       = timeline.HitMask,
                    DebugDraw     = _debugDraw,
                    DebugDuration = _debugDuration,
                };

                _runner.Begin(timeline, _timelineCtx);
            }
            else
            {
                _phaseTimer = _activeStep.ActiveTime;

                var info = new DamageInfo(
                    _activeStep.Damage,
                    _activeStep.ArmorPenetration,
                    _activeStep.DamageType,
                    _activeStep.CriticalMultiplier,
                    _damageSource,
                    _activeStep.OnHitEffects);

                _resolver.Begin(_activeStep, info, _data.HitMask, transform.root);
            }
        }

        private void ExitActive()
        {
            if (_usingTimeline)
                _runner.Stop();
            else
                _resolver.End();

            bool hitAnything = _usingTimeline ? _runner.HitAnything : _resolver.HitAnything;
            if (hitAnything)
                _activeStep.HitSound.TryPlay(transform.position);

            _phase      = Phase.Recovery;
            _phaseTimer = _activeStep.RecoveryTime;
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
    }
}
