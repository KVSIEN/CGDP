using UnityEngine;

namespace CGD.Enemies
{
    // Whether an enemy's target is looking its way right now: facing within an angle of it, as
    // the enemy sees it. Only while it senses the target — a target it can't see gives nothing
    // away — so horror that plays on being watched (freezing, glitching) stays honest.
    internal static class Gaze
    {
        public static bool IsLookedAt(EnemyAI ai, float halfAngle)
        {
            EnemyPerception perception = ai.Perception;
            if (perception == null || !perception.IsSensing) return false;

            Vector3 facing = perception.TargetFacing;
            if (facing.sqrMagnitude < 0.01f) return false;

            Vector3 toSelf = ai.transform.position - perception.Target.transform.position;
            toSelf.y = 0f;
            return toSelf.sqrMagnitude < 0.01f || Vector3.Angle(facing, toSelf) <= halfAngle;
        }
    }
}
