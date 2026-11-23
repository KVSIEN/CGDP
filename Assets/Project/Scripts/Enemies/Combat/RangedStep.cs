using System;
using UnityEngine;
using CGD.Weapons;

namespace CGD.Enemies
{
    // One step of a ranged combo (EnemyData › Ranged Combo): a burst of its own — how many shots,
    // what they are (hitscan, a projectile or a lobbed grenade), how wild, and its own cast time
    // (Windup: it stands still, unless the step Fires On The Move) and decompress time (Recovery:
    // it may move but casts nothing).
    // An enemy without a combo fires single bursts built from its Ranged Combat fields.
    [Serializable]
    public struct RangedStep
    {
        [Tooltip("What the step is, for the Inspector and the enemy review (\"bullet burst\", \"mortar strikes\")")]
        public string Name;
        [Tooltip("Shots in this step")]
        [Min(1)] public int Shots;
        [Tooltip("Seconds between its shots")]
        [Min(0f)] public float Interval;
        [Tooltip("Seconds it stands still and charges before the first shot (the cast time)")]
        [Min(0f)] public float Windup;
        [Tooltip("Seconds after the last shot it decompresses: free to move, casting nothing. Within a combo the next step waits for it")]
        [Min(0f)] public float Recovery;
        [Tooltip("Keeps walking through this step's cast and shots (Suppress and Hold Range don't stop it), so it weaves the step into its footwork. Off = it plants itself to fire")]
        public bool FiresOnTheMove;
        [Tooltip("Spread angle in degrees (for a grenade, how far off it falls per metre of range)")]
        [Min(0f)] public float SpreadAngle;
        [Tooltip("Damage of each hitscan or projectile shot. 0 = the Ranged Attack Damage. A grenade carries its own")]
        [Min(0f)] public float Damage;
        [Tooltip("Each shot is this grenade lobbed at the target (mortar strikes). Overrides Projectile. Empty = off")]
        public GrenadeData Grenade;
        [Tooltip("Prefab with a Projectile component (a missile). Empty, with no Grenade = hitscan")]
        public GameObject Projectile;
        [Tooltip("Metres per second a projectile flies at. 0 = the Shot Speed")]
        [Min(0f)] public float ShotSpeed;
        [Tooltip("Metres: thickness of a projectile, so near misses still hit")]
        [Min(0f)] public float ShotRadius;
        [Tooltip("Metres: a projectile also hurts everyone this close to where it lands (a missile's blast)")]
        [Min(0f)] public float BlastRadius;
    }
}
