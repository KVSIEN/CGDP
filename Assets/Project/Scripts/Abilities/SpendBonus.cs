using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    // A status effect a Surge-scaled ability adds once a cast spends at least AtSpend (a share of the gauge).
    [Serializable]
    public class SpendBonus
    {
        [Range(0f, 1f)] public float AtSpend = 0.5f;
        public StatusEffectApplication Effect;
    }
}
