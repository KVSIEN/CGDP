using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Player
{
    // Runs one dodge through its stages: where it's heading, how fast, whether it's
    // waiting for a follow-up press, and when it's over. Plain C#; directions are on the
    // ground plane (x = world x, y = world z). PlayerDodge feeds it input and applies the
    // velocity it asks for.
    public class DodgeMotion
    {
        private const float InputDeadzone = 0.1f;

        private IReadOnlyList<DodgeStage> _stages;
        private int _stage = -1;
        private float _time;
        private bool _waiting;
        private bool _followUpQueued;

        // The cooldown to start, given when a dodge ends (or is cut short).
        public event Action<float> Ended;

        public bool IsActive => _stage >= 0;
        public DodgeStage Stage => IsActive ? _stages[_stage] : null;
        public int StageIndex => _stage;
        public Vector2 Direction { get; private set; }

        // Moving the player right now (not between stages waiting for a press).
        public bool IsMoving => IsActive && !_waiting;
        public bool IsCommitted => IsMoving && Stage.Commits;
        public bool IsInvulnerable => IsMoving && Stage.IsInvulnerableAt(_time);
        public bool IsWaitingForFollowUp => IsActive && _waiting;
        public DodgePose Pose => IsMoving ? Stage.Pose : DodgePose.None;

        public void Start(IReadOnlyList<DodgeStage> stages, Vector2 direction)
        {
            if (stages == null || stages.Count == 0) return;
            _stages = stages;
            Direction = direction.normalized;
            BeginStage(0, Vector2.zero);
        }

        // A Dodge press while dodging: continues into the next stage if this one allows it.
        public void PressDodge()
        {
            if (IsActive && Stage.Advance == DodgeAdvance.OnDodgePress && _stage + 1 < _stages.Count)
                _followUpQueued = true;
        }

        // Stuns, mantles and deaths stop a dodge; the cooldown of the current stage applies.
        public void Cancel()
        {
            if (IsActive) End();
        }

        // Advances by dt. Returns true with the horizontal velocity to impose while a stage
        // is moving the player; false when normal movement has control.
        public bool Tick(float dt, Vector2 input, out Vector2 velocity)
        {
            velocity = Vector2.zero;
            if (!IsActive) return false;

            _time += dt;
            DodgeStage stage = Stage;

            if (!_waiting)
            {
                if (_time <= stage.Duration)
                {
                    Steer(stage, input, dt);
                    velocity = Direction * (stage.Speed * stage.SpeedCurve.Evaluate(_time / stage.Duration));
                    return true;
                }

                if (stage.Advance == DodgeAdvance.Automatic) return Next(input);
                _waiting = true;
                _time = 0f;
            }

            if (_followUpQueued) return Next(input);
            if (_time >= stage.FollowUpWindow) End();
            return false;
        }

        private bool Next(Vector2 input)
        {
            if (_stage + 1 >= _stages.Count)
            {
                End();
                return false;
            }
            BeginStage(_stage + 1, input);
            return false;
        }

        private void BeginStage(int index, Vector2 input)
        {
            _stage = index;
            _time = 0f;
            _waiting = false;
            _followUpQueued = false;
            if (Stage.Redirect && input.sqrMagnitude > InputDeadzone * InputDeadzone)
                Direction = input.normalized;
        }

        private void Steer(DodgeStage stage, Vector2 input, float dt)
        {
            if (stage.Steering == DodgeSteering.Locked || input.sqrMagnitude <= InputDeadzone * InputDeadzone) return;

            Vector2 target = input.normalized;
            if (stage.Steering == DodgeSteering.Free)
            {
                Direction = target;
                return;
            }
            Direction = RotateTowards(Direction, target, stage.TurnRate * dt);
        }

        private static Vector2 RotateTowards(Vector2 from, Vector2 to, float maxDegrees)
        {
            float angle = Mathf.Atan2(from.x * to.y - from.y * to.x, from.x * to.x + from.y * to.y) * Mathf.Rad2Deg;
            float step = Mathf.Clamp(angle, -maxDegrees, maxDegrees) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(step), sin = Mathf.Sin(step);
            return new Vector2(from.x * cos - from.y * sin, from.x * sin + from.y * cos);
        }

        private void End()
        {
            float cooldown = Stage.Cooldown;
            _stage = -1;
            _waiting = false;
            _followUpQueued = false;
            Ended?.Invoke(cooldown);
        }
    }
}
