using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while its target is looking its way. For horror that only moves when unwatched,
    // freezes under a stare or bolts the moment the target turns away.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Being Watched", "Succeeds while its target is looking its way (as far as it can see).")]
    public sealed class BeingWatched : EnemyCondition
    {
        [Tooltip("Degrees either side of where the target faces that count as looking at it")]
        [SerializeField, Range(5f, 90f)] private float _angle = 35f;

        public override string Summary => $"Within {_angle:0}°";

        protected override bool Check(BtContext context) => Gaze.IsLookedAt(Ai, _angle);
    }
}
