using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while an ally of its own it saw or heard fall moments ago, close by, has not been
    // avenged yet — and it knows who did it. Put Avenge Ally after it.
    [Serializable]
    [BtNodeInfo("Enemy/Senses/Ally Fell", "Succeeds while an ally it saw or heard fall nearby, moments ago, is unavenged.")]
    public sealed class AllyFell : EnemyCondition
    {
        [Tooltip("Seconds since the death that still count")]
        [SerializeField, Min(0f)] private float _within = 5f;
        [Tooltip("Metres from where the ally fell")]
        [SerializeField, Min(1f)] private float _radius = 25f;

        public override string Summary => $"Within {_within:0.#} s, {_radius:0} m";

        protected override bool Check(BtContext context) => Ai.Perception.HasUnavengedFall(_within, _radius, out _);
    }
}
