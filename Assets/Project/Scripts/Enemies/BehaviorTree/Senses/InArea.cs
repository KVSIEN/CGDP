using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it — or where it believes its target is — is within a radius of its post
    // (where it was placed or spawned). The leash for defenders: fight what comes into the area,
    // never chase past its edge, go back when pulled out of it.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/In Area", "Succeeds while it (or its target) is within a radius of its post.")]
    public sealed class InArea : EnemyCondition
    {
        public enum Subject { Self, Target }

        [SerializeField] private Subject _subject = Subject.Self;
        [Tooltip("Metres round its post")]
        [SerializeField, Min(0.5f)] private float _radius = 12f;

        public override string Summary => $"{_subject} within {_radius:0} m of post";

        protected override bool Check(BtContext context)
        {
            if (_subject == Subject.Self) return Ai.Post.IsNear(Ai.transform.position, _radius);
            return Ai.Perception.Target != null && Ai.Post.IsNear(Ai.Perception.TargetPosition, _radius);
        }
    }
}
