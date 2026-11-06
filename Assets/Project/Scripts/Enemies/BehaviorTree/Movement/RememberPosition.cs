using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Stores a position on the blackboard for later — "where I was before I chased", "where
    // the noise came from" — to come back to with Move To. Fails if it has no target to
    // remember a target position of.
    [Serializable]
    [BtNodeInfo("Enemy/Movement/Remember Position", "Stores a position (itself, post, target, disturbance) on the blackboard, then succeeds.")]
    public sealed class RememberPosition : EnemyAction
    {
        [SerializeField] private string             _key = "Spot";
        [SerializeField] private RememberedPosition _what = RememberedPosition.Self;

        public override string Summary => $"{_what} → {_key}";

        protected override BtStatus OnTick(BtContext context)
        {
            var perception = Ai.Perception;
            Vector3 position;
            switch (_what)
            {
                case RememberedPosition.Post:        position = Ai.Post.Position;          break;
                case RememberedPosition.Disturbance: position = perception.LookPosition;   break;
                case RememberedPosition.Target:
                    if (perception.Target == null) return BtStatus.Failure;
                    position = perception.TargetPosition;
                    break;
                default:                             position = Ai.transform.position;     break;
            }

            context.Blackboard.SetVector(_key, position);
            return BtStatus.Success;
        }
    }
}
