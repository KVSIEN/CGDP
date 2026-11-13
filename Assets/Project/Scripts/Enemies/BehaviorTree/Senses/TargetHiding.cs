using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Stealth;

namespace CGD.Enemies
{
    // Succeeds while its target has gone to ground somewhere it knows: out of its senses for no
    // more than a few seconds, and where it was last seen is a hiding place — inside a bush, or
    // behind something solid from here. The cue to flush it out (a grenade, fire into the spot)
    // rather than walk round to look. Only uses what the enemy saw: where the target went, not
    // where it is now.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Target Hiding", "Succeeds while its target was lost moments ago in a bush or behind cover from here.")]
    public sealed class TargetHiding : EnemyCondition
    {
        private const float ChestHeight = 1.1f;
        private const float GrassHeight = 0.5f;   // a crouching target's middle, for low cover it hides in

        [Tooltip("Seconds since it last sensed the target that still count as knowing where it hides")]
        [SerializeField, Min(0f)] private float _within = 10f;
        [Tooltip("Counts a bush (or tall grass) where it was last seen")]
        [SerializeField] private bool _inBushes = true;
        [Tooltip("Counts a wall or other solid cover between this enemy and where it was last seen")]
        [SerializeField] private bool _behindCover = true;

        public override string Summary => $"Lost under {_within:0.#} s ago";

        protected override bool Check(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (perception.Target == null || perception.IsSensing) return false;
            if (perception.Level == AwarenessLevel.Unaware || perception.TimeSinceSensed > _within) return false;

            // Where it last saw the target, or its guess at where that was heading (a step on).
            Vector3 spot = perception.LastKnownPosition;
            if (_inBushes && (InBush(spot) || InBush(perception.LookPosition))) return true;
            if (!_behindCover) return false;

            Vector3 eye = Ai.transform.position + Vector3.up * EnemyEyes.Height;
            return !SightLines.IsClear(eye, spot + Vector3.up * ChestHeight, Ai.ObstacleMask, perception.Target.transform);
        }

        private static bool InBush(Vector3 spot) => ConcealmentZone.AnyContains(spot + Vector3.up * GrassHeight, crouching: true);
    }
}
