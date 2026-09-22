using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    public class TimelineAbilityRunner : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool  _debugDraw     = true;
        [SerializeField] private float _debugDuration = 0.3f;

        private readonly ActionTimelineRunner _runner = new();
        private ActionContext _ctx;
        private Transform _cameraTransform;

        public bool IsPlaying => _runner.IsRunning;

        public void Play(ActionTimeline timeline, AbilityContext abilityCtx)
        {
            _cameraTransform = abilityCtx.CameraTransform;

            _ctx = new ActionContext
            {
                Origin        = _cameraTransform.position,
                Forward       = _cameraTransform.forward,
                Up            = _cameraTransform.up,
                SourceRoot    = abilityCtx.PlayerTransform.root,
                Source        = abilityCtx.Source,
                HitMask       = timeline.HitMask,
                DebugDraw     = _debugDraw,
                DebugDuration = _debugDuration,
            };

            _runner.Begin(timeline, _ctx);
        }

        private void FixedUpdate()
        {
            if (!_runner.IsRunning) return;

            if (_cameraTransform != null)
            {
                _ctx.Origin  = _cameraTransform.position;
                _ctx.Forward = _cameraTransform.forward;
                _ctx.Up      = _cameraTransform.up;
            }

            _runner.Tick();
        }

        private void OnDisable()
        {
            _runner.Stop();
        }
    }
}
