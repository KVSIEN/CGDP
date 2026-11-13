using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it sees its target coming at it: within range and moving its way at least
    // this fast. The cue for a kiter to back off before it is caught.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Target Closing In", "Succeeds while it sees its target moving toward it, within range.")]
    public sealed class TargetClosingIn : EnemyCondition
    {
        [Tooltip("Metres: only counts within this range")]
        [SerializeField, Min(0.5f)] private float _range = 12f;
        [Tooltip("Metres per second it must be closing at")]
        [SerializeField, Min(0f)] private float _speed = 1.5f;

        public override string Summary => $"≥ {_speed:0.#} m/s within {_range:0} m";

        protected override bool Check(BtContext context)
        {
            EnemyPerception perception = Ai.Perception;
            if (!perception.IsSensing) return false;

            Vector3 toSelf = Ai.transform.position - perception.Target.transform.position;
            toSelf.y = 0f;
            float distance = toSelf.magnitude;
            if (distance > _range || distance < 0.01f) return false;

            Vector3 velocity = perception.TargetVelocity;
            velocity.y = 0f;
            return Vector3.Dot(velocity, toSelf / distance) >= _speed;
        }
    }
}
