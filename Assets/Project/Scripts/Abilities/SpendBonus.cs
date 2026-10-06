using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Abilities
{
    // A status effect a resource-scaled ability adds once a cast spends at least AtSpend.
    [Serializable]
    public class SpendBonus
    {
        [Min(0f)] public float AtSpend = 50f;
        public StatusEffectApplication Effect;
    }
}
