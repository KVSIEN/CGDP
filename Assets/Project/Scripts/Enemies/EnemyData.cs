using System.Collections.Generic;
using UnityEngine;
using CGD.Abilities;
using CGD.Audio;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Factions;
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

        [Tooltip("Above 0, this enemy anchors its squad: the rest of its team keep within about this many metres of it " +
                 "(cover is chosen near it, and the Anchor Tether and Return To Anchor nodes pull strays back). 0 = not an anchor")]
        [Min(0f)] public float AnchorRadius = 0f;

        [Header("Health")]
        [Tooltip("0 = no health at all: it lives on its plating and dies when that runs out (TECH)")]
        public float MaxHealth = 100f;
        public float Armor     = 0f;

        [Header("Plating")]
        [Tooltip("Armour plating (a TECH hull, a BIO's bone plates): between shield and health, never recovers by itself, only by repair (a Restore Plating ability)")]
        [Min(0f)] public float MaxPlating = 0f;
        [Tooltip("Armour the plates add, on top of Armor, against hits that land on them")]
        [Min(0f)] public float PlatingArmor = 0f;

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
        [Tooltip("How its faction treats the others: whom it hunts, avoids or flees, and whether rivals leave each other be while calm. Empty = it fights every hostile alike")]
        public FactionRelations Relations;

        [Tooltip("How its faction carries itself: what shakes its nerve, how it idles, what it gives away before a fight. Empty = a plain enemy")]
        public FactionDoctrine Doctrine;

        [Tooltip("Wildlife and service bots: the director does not count it as a threat. Hurting or killing it adds no stress and is no combat, and the director never picks it to probe or to speak")]
        public bool OutsidePacing;

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
        [Tooltip("Status effects it shrugs off (a TECH machine doesn't bleed)")]
        public StatusEffect[] StatusImmunities;
        [Tooltip("Status effects every melee hit applies (an Infector's infestation, a venomous bite). Empty = none")]
        public StatusEffectApplication[] MeleeEffects;
        [Tooltip("Seconds between starting an attack and the hit landing (melee only). Ignored when a Melee Combo is set")]
        public float AttackWindup   = 0.35f;
        [Tooltip("A melee swing made of several blows in turn (an ice mace's three heavy hits), each with its own wind-up, damage share and area. Empty = one blow as above. Attack Cooldown runs after the last")]
        public MeleeBlow[] MeleeCombo;
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
        [Tooltip("Seconds it stands and charges before a burst's first shot (a cast time). While winding up and recovering it casts no abilities or grenades. 0 = fires at once")]
        [Min(0f)] public float BurstWindup   = 0f;
        [Tooltip("Seconds after a burst's last shot before it may cast anything again — free to reposition and plan. 0 = none")]
        [Min(0f)] public float BurstRecovery = 0f;
        [Tooltip("A basic attack made of several bursts in turn (a bullet burst, then mortar strikes, then a missile), each with its own shots, spread, wind-up and recovery. " +
                 "Between steps it casts nothing. The Ranged Attack Cooldown runs after the last step. Empty = single bursts from the fields above (Burst Count to Shot Grenade)")]
        public RangedStep[] RangedCombo;
        [Tooltip("How often the enemy picks a new strafe direction (seconds)")]
        public float StrafeInterval = 2f;
        [Tooltip("How far sideways the enemy strafes at preferred range")]
        public float StrafeDistance  = 4f;
        [Tooltip("Prefab with a Projectile component: shots fly as visible, dodgeable rounds. Empty = instant hitscan")]
        public GameObject ShotProjectile;
        [Tooltip("Each shot of a burst is this grenade lobbed at the target instead (a mortar volley): the grenade carries the damage, blast and fuse, Spread Angle becomes how far off it falls. Overrides Shot Projectile. Empty = off")]
        public GrenadeData ShotGrenade;
        [Tooltip("Metres per second a projectile shot flies at")]
        [Min(1f)] public float ShotSpeed = 30f;
        [Tooltip("Metres: thickness of a projectile shot, so near misses still hit. 0 = a thin ray")]
        [Min(0f)] public float ShotRadius = 0f;
        [Tooltip("Metres: a projectile shot also hurts everyone this close to where it lands. 0 = none")]
        [Min(0f)] public float ShotBlastRadius = 0f;
        [Tooltip("Status effects every ranged hit applies (Void's slow)")]
        public StatusEffectApplication[] ShotEffects;
        [Tooltip("What kind of damage each ranged shot deals (True = armour does nothing against it, a curse's bolt)")]
        public DamageType ShotDamageType = DamageType.Physical;
        [Tooltip("Prefab with a DamageZone (Prefabs/VFX/DamageZone): a projectile shot leaves a patch of harmful ground where it lands. Empty = none")]
        public GameObject ShotZonePrefab;
        [Tooltip("What that patch does (radius, how long it lasts, damage per tick, look). A radius of 0 = no patch. It applies the Shot Effects too")]
        public ZoneSettings ShotZone = new() { Radius = 0f };

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
        [Tooltip("Its faction's own trick (Bash, Primal Roar, Siphon), for ability nodes set to the Special slot. Empty = none")]
        public Ability Special;

        [Header("Alert")]
        [Tooltip("Longest it spends investigating something before it gives up and goes back to patrol (2.5× when hunting a hostile it knows is about).")]
        public float AlertDuration = 5f;

        [Header("Audio")]
        public SoundBank AttackSound;
        public SoundBank RangedAttackSound;

        // Raw damage it takes to kill from full: shield, plating and health together, all behind
        // armor and plating behind its own plating armor too (Effective Damage = Raw × 100 /
        // (100 + Armor), GDD › Damage Calculation). Low-tier enemies sit around 100; each reality
        // spreads it differently.
        // The kit ability in a slot (null for Own or an empty slot).
        public Ability KitAbility(AbilitySlot slot) => slot switch
        {
            AbilitySlot.Signature => Signature,
            AbilitySlot.Escape    => Escape,
            AbilitySlot.Summon    => Summon,
            AbilitySlot.Boost     => Boost,
            AbilitySlot.Support   => Support,
            AbilitySlot.Special   => Special,
            _                     => null,
        };

        // What a behavior node that names a slot casts: its own ability for Own, else the kit's.
        public Ability KitAbility(AbilitySlot slot, Ability own) => slot == AbilitySlot.Own ? own : KitAbility(slot);

        // The distance it fights its target from: where a ranged enemy holds, or just past where a
        // melee one swings. Falls into one of the CombatRange bands.
        public float EngagementRange => CombatType == EnemyCombatType.Ranged ? PreferredRange : AttackRange + 2f;
        public CombatRange Range => CombatRanges.Of(EngagementRange);

        public float EffectiveHealth =>
            (MaxHealth + MaxShield) * (1f + Mathf.Max(0f, Armor) / 100f) +
            MaxPlating * (1f + (Mathf.Max(0f, Armor) + PlatingArmor) / 100f);

        // Damage per second of non-stop attacking at base speed (no buffs, every hit landing), for
        // comparing enemy types: a melee swing every wind-up + cooldown...
        public float MeleeDamagePerSecond => AttackDamage * MeleeDamageShare / Mathf.Max(0.01f, MeleeWindupTime + AttackCooldown);

        // The whole swing's damage as a multiple of Attack Damage, and the time it spends winding up.
        public float MeleeDamageShare
        {
            get
            {
                if (MeleeCombo == null || MeleeCombo.Length == 0) return 1f;
                float share = 0f;
                foreach (MeleeBlow blow in MeleeCombo) share += blow.DamageShare;
                return share;
            }
        }

        public float MeleeWindupTime
        {
            get
            {
                if (MeleeCombo == null || MeleeCombo.Length == 0) return AttackWindup;
                float time = 0f;
                foreach (MeleeBlow blow in MeleeCombo) time += blow.Windup;
                return time;
            }
        }

        // ...and a ranged burst every wind-up plus cooldown (counted from the burst's first shot), or
        // every burst's length (shots and recovery) if that takes longer. A combo runs its steps
        // back to back (each waits out the one before's recovery), then the cooldown.
        public float RangedDamagePerSecond
        {
            get
            {
                float cooldown = RangedAttackCooldown > 0f ? RangedAttackCooldown : AttackCooldown;
                if (!HasRangedCombo)
                {
                    int   shots = Mathf.Max(1, BurstCount);
                    float cycle = BurstWindup + Mathf.Max(cooldown, (shots - 1) * BurstInterval + BurstRecovery);
                    return BaseRangedDamage * shots / Mathf.Max(0.01f, cycle);
                }

                float damage = 0f, time = 0f;
                for (int i = 0; i < RangedCombo.Length; i++)
                {
                    RangedStep step = RangedCombo[i];
                    int shots = Mathf.Max(1, step.Shots);
                    damage += StepDamage(step) * shots;
                    time   += step.Windup + (shots - 1) * step.Interval;
                    // The last step's recovery overlaps the cooldown that follows it.
                    time   += i < RangedCombo.Length - 1 ? step.Recovery : Mathf.Max(cooldown, step.Recovery);
                }
                return damage / Mathf.Max(0.01f, time);
            }
        }

        public bool HasRangedCombo => RangedCombo != null && RangedCombo.Length > 0;

        public int RangedStepCount => HasRangedCombo ? RangedCombo.Length : 1;

        // A step of its basic ranged attack: its combo's, or for an enemy without one, the single
        // burst its Ranged Combat fields describe.
        public RangedStep RangedStepAt(int index)
        {
            if (HasRangedCombo) return RangedCombo[Mathf.Clamp(index, 0, RangedCombo.Length - 1)];
            return new RangedStep
            {
                Name        = "Burst",
                Shots       = BurstCount,
                Interval    = BurstInterval,
                Windup      = BurstWindup,
                Recovery    = BurstRecovery,
                SpreadAngle = SpreadAngle,
                Damage      = BaseRangedDamage,
                Grenade     = ShotGrenade,
                Projectile  = ShotProjectile,
                ShotSpeed   = ShotSpeed,
                ShotRadius  = ShotRadius,
                BlastRadius = ShotBlastRadius,
            };
        }

        // Damage of one hitscan or projectile shot of a step.
        public float StepDamage(RangedStep step) => step.Damage > 0f ? step.Damage : BaseRangedDamage;

        private float BaseRangedDamage => RangedAttackDamage > 0f ? RangedAttackDamage : AttackDamage;
    }
}
