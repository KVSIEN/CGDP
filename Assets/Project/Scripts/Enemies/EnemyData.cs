using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Audio;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Weapons;

namespace CGD.Enemies
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "CGD/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Tooltip("Shown in kill confirmations")]
        public string DisplayName = "Enemy";

        [Tooltip("Enemies never damage their own team")]
        public Team Team = Team.Enemy;

        [Tooltip("1 = rank and file; higher tiers are tougher and roll more random affixes (see EnemyAffixPool)")]
        [Range(1, 3)] public int Tier = 1;

        [Header("Health")]
        public float MaxHealth = 100f;
        public float Armor     = 0f;

        [Header("Shield")]
        public float MaxShield = 0f;
        [Tooltip("Seconds without taking damage before shield starts regenerating")]
        public float ShieldRegenDelay = 5f;
        [Tooltip("Shield points restored per second once regen starts")]
        public float ShieldRegenRate = 10f;

        [Header("Movement")]
        public float PatrolSpeed = 2f;
        public float ChaseSpeed  = 5f;
        [Tooltip("Seconds to get up to speed, or stop, from whatever pace it is moving at. Lower = snappier turns and stops, less sliding at high speed")]
        [Min(0.02f)] public float MoveResponseTime = 0.15f;
        [Tooltip("When sneaking (roaming, stalking, searching, heading home) it passes through bushes on the way if that " +
                 "makes the walk at most this much longer: 0.5 = up to 50% further. 0 = always the direct way. Rushes and dodges stay direct")]
        [Range(0f, 2f)] public float BushDetour = 0f;

        [Header("Detection")]
        [Tooltip("Maximum sight distance in metres.")]
        public float SightRange = 15f;
        [Tooltip("Full cone angle in degrees — e.g. 90 means 45° either side of forward.")]
        public float SightAngle = 90f;
        [Tooltip("How far it hears a walking hostile's footsteps (crouching quieter, sprinting further, standing still silent; walls halve it). Gunfire and other noises carry their own radius.")]
        public float HearingRadius = 8f;

        [Tooltip("How its senses, memory and reactions are tuned. Empty = built-in defaults")]
        public PerceptionProfile Senses;
        [Tooltip("Whom it would rather fight when it senses several hostiles (rival factions): the weakest, whoever hurt it, whoever is busy elsewhere. Empty = it just sticks with its current target")]
        public TargetPriorities TargetPriorities;

        [Tooltip("How its faction carries itself: what shakes its nerve, how it idles, what it gives away before a fight. Empty = a plain enemy")]
        public FactionDoctrine Doctrine;

        [Header("Behavior")]
        [Tooltip("Decides what the enemy does: patrolling, searching, fighting (edit it in CGD › AI › Behavior Tree Editor). " +
                 "Without one the enemy just stands there")]
        public BehaviorTreeAsset BehaviorTree;
        [Tooltip("This enemy type's own take on the behaviors in its tree: some switched off, some with their own settings — " +
                 "so it can share a tree with other types and still differ (edit it in the Behavior Composer, 'Only this enemy')")]
        public List<BehaviorTuning> BehaviorTuning = new();
        [Tooltip("What it says out loud (spotted you, flanking, falling back…). Empty = silent")]
        public EnemyBarkSet Voice;

        [Header("Combat")]
        public EnemyCombatType CombatType = EnemyCombatType.Melee;
        [Tooltip("How it relates to its kin: a union that shares what it sees, a loner, a herd (see SocialStyle)")]
        public SocialStyle Social = SocialStyle.Standard;
        [Tooltip("Other teams it counts as kin: it hears their calls and a herd stays near them (a pact)")]
        public Team[] Pacts;
        public float AttackRange    = 1.5f;
        public float AttackDamage   = 15f;
        public float AttackCooldown = 1f;
        [Tooltip("Seconds between ranged bursts. 0 = the attack cooldown (a hybrid keeps its melee quick and its shooting slow)")]
        [Min(0f)] public float RangedAttackCooldown = 0f;
        [Tooltip("Damage of each ranged shot. 0 = the attack damage (a hybrid can shoot harder or softer than it stabs)")]
        [Min(0f)] public float RangedAttackDamage = 0f;
        [Tooltip("Status effects every melee hit applies (an Infector's infestation, a venomous bite). Empty = none")]
        public StatusEffectApplication[] MeleeEffects;
        [Tooltip("Seconds between starting an attack and the hit landing (melee only)")]
        public float AttackWindup   = 0.35f;
        [Tooltip("Colour of the damage numbers on whatever this enemy hurts, to tell attackers apart (rival factions). Clear = the usual white")]
        public Color DamageNumberColor = Color.clear;

        [Header("Ranged Combat")]
        [Tooltip("Distance the enemy tries to maintain from its target")]
        public float PreferredRange = 12f;
        [Tooltip("Spread angle in degrees — 0 is perfect accuracy")]
        public float SpreadAngle    = 3f;
        [Tooltip("Shots fired per burst before the fire cooldown starts")]
        public int   BurstCount     = 1;
        [Tooltip("Delay in seconds between shots within a burst")]
        public float BurstInterval  = 0.1f;
        [Tooltip("How often the enemy picks a new strafe direction (seconds)")]
        public float StrafeInterval = 2f;
        [Tooltip("How far sideways the enemy strafes at preferred range")]
        public float StrafeDistance  = 4f;
        [Tooltip("Prefab with a Projectile component: shots fly as visible, dodgeable rounds. Empty = instant hitscan")]
        public GameObject ShotProjectile;
        [Tooltip("Metres per second a projectile shot flies at")]
        [Min(1f)] public float ShotSpeed = 30f;
        [Tooltip("Metres: thickness of a projectile shot, so near misses still hit. 0 = a thin ray")]
        [Min(0f)] public float ShotRadius = 0f;
        [Tooltip("Metres: a projectile shot also hurts everyone this close to where it lands. 0 = none")]
        [Min(0f)] public float ShotBlastRadius = 0f;
        [Tooltip("Status effects every ranged hit applies (Void's slow)")]
        public StatusEffectApplication[] ShotEffects;

        [Header("Group & Reactions")]
        [Tooltip("It won't step in to strike while this many allies (itself included) are already in melee with the same target — it circles instead")]
        [Min(1)] public int MaxMeleeAttackers = 2;
        [Tooltip("It holds fire while this many allies are already firing bursts at the same target")]
        [Min(1)] public int MaxShooters = 3;
        [Tooltip("A single hit taking at least this share of max health staggers it (interrupting a wind-up)")]
        [Range(0.01f, 1f)] public float StaggerThreshold = 0.25f;
        [Tooltip("Seconds a stagger lasts")]
        [Min(0f)] public float StaggerDuration = 0.4f;
        [Tooltip("Seconds before it can be staggered again")]
        [Min(0f)] public float StaggerCooldown = 2.5f;
        [Tooltip("On first spotting a hostile, runs to an alarm panel near its post to raise the alarm, if it knows one")]
        public bool RaisesAlarm;

        [Header("Kit")]
        [Tooltip("What Throw Grenade nodes that name no grenade of their own throw (shared behaviors like Flush Out). Empty = none")]
        public GrenadeData Grenade;
        [Tooltip("Its signature attack, for ability nodes set to the Signature slot. Empty = none")]
        public Ability Signature;
        [Tooltip("How it gets away, for ability nodes set to the Escape slot. Empty = none")]
        public Ability Escape;
        [Tooltip("What it calls up (a Summon ability), for ability nodes set to the Summon slot. Empty = none")]
        public Ability Summon;
        [Tooltip("A buff on itself (Fortify, Surge…), for ability nodes set to the Boost slot. Empty = none")]
        public Ability Boost;
        [Tooltip("What it does for allies (a Repair: a heal on allies around it), for Support Ally and ability nodes set to the Support slot. Empty = none")]
        public Ability Support;

        [Header("Alert")]
        [Tooltip("Longest it spends investigating something before it gives up and goes back to patrol (2.5× when hunting a hostile it knows is about).")]
        public float AlertDuration = 5f;

        [Header("Audio")]
        public SoundBank AttackSound;
        public SoundBank RangedAttackSound;

        // Raw damage it takes to kill from full: shield and health together, both behind
        // armor (Effective Damage = Raw × 100 / (100 + Armor), GDD › Damage Calculation).
        // Low-tier enemies sit around 100; each reality spreads it differently.
        // The kit ability in a slot (null for Own or an empty slot).
        public Ability KitAbility(AbilitySlot slot) => slot switch
        {
            AbilitySlot.Signature => Signature,
            AbilitySlot.Escape    => Escape,
            AbilitySlot.Summon    => Summon,
            AbilitySlot.Boost     => Boost,
            AbilitySlot.Support   => Support,
            _                     => null,
        };

        public float EffectiveHealth => (MaxHealth + MaxShield) * (1f + Mathf.Max(0f, Armor) / 100f);

        // Damage per second of non-stop attacking at base speed (no buffs, every hit landing), for
        // comparing enemy types: a melee swing every wind-up + cooldown...
        public float MeleeDamagePerSecond => AttackDamage / Mathf.Max(0.01f, AttackWindup + AttackCooldown);

        // ...and a ranged burst every cooldown (counted from the burst's first shot), or every
        // burst's length if the shots take longer than that.
        public float RangedDamagePerSecond
        {
            get
            {
                int   shots    = Mathf.Max(1, BurstCount);
                float damage   = RangedAttackDamage > 0f ? RangedAttackDamage : AttackDamage;
                float cooldown = RangedAttackCooldown > 0f ? RangedAttackCooldown : AttackCooldown;
                float cycle    = Mathf.Max(cooldown, (shots - 1) * BurstInterval);
                return damage * shots / Mathf.Max(0.01f, cycle);
            }
        }
    }
}
