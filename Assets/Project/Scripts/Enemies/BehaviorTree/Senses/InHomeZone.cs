using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while it — or where it believes its target is — is inside its home territory (the
    // rooms its faction holds around where it was placed), give or take a margin. Where In Area
    // leashes to a post, this follows the territory the level placed it in. Without a home (placed by hand) it always succeeds: nothing to be outside of.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/In Home Zone", "Succeeds while it (or its target) is inside its home territory, give or take a margin.")]
    public sealed class InHomeZone : EnemyCondition
    {
        public enum Subject { Self, Target }

        [SerializeField] private Subject _subject = Subject.Self;
        [Tooltip("Metres past the room's walls that still count as inside")]
        [SerializeField, Min(0f)] private float _margin;

        public override string Summary => $"{_subject} within {_margin:0} m of home territory";

        protected override bool Check(BtContext context)
        {
            if (_subject == Subject.Self) return Ai.IsWithinHome(Ai.transform.position, _margin);
            return Ai.Perception.Target != null && Ai.IsWithinHome(Ai.Perception.TargetPosition, _margin);
        }
    }
}
