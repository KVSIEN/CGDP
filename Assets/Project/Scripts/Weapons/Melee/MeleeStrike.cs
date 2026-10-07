using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    // Resolves the hits of one strike's Active phase: the step's ActionTimeline when it has one,
    // otherwise the hit resolver's shape casts. Follows the camera every physics tick.
    public sealed class MeleeStrike
    {
        private readonly MeleeHitResolver     _resolver = new();
        private readonly ActionTimelineRunner _runner   = new();
        private ActionContext _context;
        private bool _usingTimeline;

        public bool UsesTimeline => _usingTimeline;

        // A timeline strike authors its own damage per event, so the wielder's buffs reach it as
        // `damageScale`. Returns how long the timeline runs.
        public float BeginTimeline(ActionTimeline timeline, Transform aim, Transform sourceRoot, DamageSource source,
                                   float damageScale, bool debugDraw, float debugDuration)
        {
            _usingTimeline = true;
            _context = new ActionContext
            {
                Origin        = aim.position,
                Forward       = aim.forward,
                Up            = aim.up,
                SourceRoot    = sourceRoot,
                Source        = source,
                DamageScale   = damageScale,
                HitMask       = timeline.HitMask,
                DebugDraw     = debugDraw,
                DebugDuration = debugDuration,
            };
            _runner.Begin(timeline, _context);
            return timeline.TotalFrames * Time.fixedDeltaTime;
        }

        public void BeginShapes(MeleeAttackStep step, DamageInfo info, LayerMask hitMask, Transform sourceRoot, float reach)
        {
            _usingTimeline = false;
            _resolver.Begin(step, info, hitMask, sourceRoot, reach);
        }

        // One physics tick. Returns false once a timeline has finished.
        public bool Tick(Transform aim, bool debugDraw, float debugDuration)
        {
            if (!_usingTimeline)
            {
                _resolver.Tick(aim.position, aim.forward, aim.up, debugDraw, debugDuration);
                return true;
            }

            _context.Origin  = aim.position;
            _context.Forward = aim.forward;
            _context.Up      = aim.up;
            _runner.Tick();
            return _runner.IsRunning;
        }

        // Stops the strike; true if it hit anything.
        public bool End()
        {
            if (_usingTimeline) _runner.Stop();
            else                _resolver.End();
            return _usingTimeline ? _runner.HitAnything : _resolver.HitAnything;
        }
    }
}
