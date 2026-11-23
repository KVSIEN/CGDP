using System;
using UnityEngine;

namespace CGD.Enemies
{
    // One blow of a melee combo (EnemyData › Melee Combo): how long it winds up, how hard it hits
    // (a share of the attack damage) and the ball of space it lands in, centred Reach metres in
    // front of the attacker — a wide Radius makes it an area blow (a hammer's slam).
    [Serializable]
    public struct MeleeBlow
    {
        [Tooltip("Seconds it winds up before this blow lands (the whole combo is one swing: the cooldown starts after the last)")]
        [Min(0.05f)] public float Windup;
        [Tooltip("Share of the attack damage this blow deals (1 = the full Attack Damage)")]
        [Min(0f)] public float DamageShare;
        [Tooltip("Metres in front of it where the blow lands")]
        [Min(0f)] public float Reach;
        [Tooltip("Metres round that point that it hits everyone in")]
        [Min(0.1f)] public float Radius;
        [Tooltip("Whether the blow applies EnemyData › Melee Effects (a hammer's chill on its heavier blows)")]
        public bool AppliesEffects;
    }
}
