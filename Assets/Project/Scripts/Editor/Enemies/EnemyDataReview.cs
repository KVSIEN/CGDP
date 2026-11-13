using System;
using System.Collections.Generic;
using CGD.Combat;
using CGD.Enemies;

namespace CGD.Editor
{
    // Reads an EnemyData (and the tree it thinks with) back in plain words, for designers: what
    // the numbers add up to, and settings that probably don't do what was meant — an enemy that
    // can never shoot, one that holds a range it can't see from. Used by the EnemyData inspector
    // and the Enemy Roster window.
    internal static class EnemyDataReview
    {
        private static readonly Type[] ShootingNodes = { typeof(ShootBurst), typeof(Suppress) };
        private static readonly Type[] MeleeNodes    = { typeof(MeleeAttack) };
        private static readonly Type[] AttackNodes   = { typeof(ShootBurst), typeof(Suppress), typeof(MeleeAttack), typeof(UseAbility), typeof(ThrowGrenade) };

        public readonly struct Issue
        {
            public readonly BtIssueSeverity Severity;
            public readonly string          Message;

            public Issue(BtIssueSeverity severity, string message)
            {
                Severity = severity;
                Message  = message;
            }
        }

        // One line per aspect: toughness, each attack it has, senses, speed.
        public static List<string> Summary(EnemyData data)
        {
            var lines = new List<string>();
            if (data == null) return lines;

            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(data.BehaviorTree);
            lines.Add($"Toughness: {data.EffectiveHealth:0} raw damage to kill ({data.MaxHealth:0} health + {data.MaxShield:0} shield, {data.Armor:0} armour).");

            if (MeleeAttacks(data, nodes))
                lines.Add($"Melee: {data.AttackDamage:0.#} per hit, about {data.MeleeDamagePerSecond:0.#} damage/s " +
                          $"(reach {data.AttackRange:0.#} m, {data.AttackWindup:0.##} s wind-up).");

            if (Shoots(data, nodes))
            {
                string shots = data.ShotProjectile != null ? $"projectiles at {data.ShotSpeed:0} m/s" : "hitscan";
                lines.Add($"Ranged: about {data.RangedDamagePerSecond:0.#} damage/s, prefers {data.PreferredRange:0} m " +
                          $"({Math.Max(1, data.BurstCount)}-shot bursts, {data.SpreadAngle:0.#}° spread, {shots}).");
            }

            lines.Add($"Senses: sees {data.SightRange:0} m in a {data.SightAngle:0}° cone, hears footsteps within {data.HearingRadius:0} m.");
            lines.Add($"Speed: walks {data.PatrolSpeed:0.#} m/s, runs {data.ChaseSpeed:0.#} m/s.");
            return lines;
        }

        public static List<Issue> Check(EnemyData data)
        {
            var issues = new List<Issue>();
            if (data == null) return issues;

            if (data.BehaviorTree == null)
            {
                issues.Add(new Issue(BtIssueSeverity.Error, "No Behavior Tree: the enemy will just stand there. Assign one under Behavior."));
                return issues;
            }

            HashSet<Type> nodes = EnemyTreeContents.NodeTypes(data.BehaviorTree);
            if (data.MaxHealth <= 0f)
                issues.Add(new Issue(BtIssueSeverity.Error, "Max Health is 0: it dies the moment it spawns."));

            if (!EnemyTreeContents.UsesAny(nodes, AttackNodes))
                issues.Add(new Issue(BtIssueSeverity.Info, "Its tree never attacks (no Melee Attack, Shoot Burst, Suppress, Use Ability or Throw Grenade). Fine for a decoy."));
            else if (data.CombatType == EnemyCombatType.Ranged && !EnemyTreeContents.UsesAny(nodes, ShootingNodes))
                issues.Add(new Issue(BtIssueSeverity.Warning, "Combat Type is Ranged, but its tree never shoots (no Shoot Burst or Suppress node)."));
            else if (data.CombatType == EnemyCombatType.Melee && EnemyTreeContents.UsesAny(nodes, ShootingNodes))
                issues.Add(new Issue(BtIssueSeverity.Info, "Its tree shoots, but Combat Type is Melee: flanking and squad roles treat it as a melee fighter. Set Ranged for a shooter or a hybrid."));

            if (Shoots(data, nodes) && data.PreferredRange > data.SightRange)
                issues.Add(new Issue(BtIssueSeverity.Warning, $"Preferred Range ({data.PreferredRange:0} m) is beyond its Sight Range ({data.SightRange:0} m): it backs off to where it can no longer see its target."));

            if (data.ShotProjectile != null && !data.ShotProjectile.TryGetComponent(out Projectile _))
                issues.Add(new Issue(BtIssueSeverity.Error, $"Shot Projectile '{data.ShotProjectile.name}' has no Projectile component, so its shots can't fly."));

            if (data.RaisesAlarm && !nodes.Contains(typeof(RaiseAlarm)))
                issues.Add(new Issue(BtIssueSeverity.Warning, "Raises Alarm is on, but its tree has no Raise Alarm node (add the shared RaiseAlarmSubtree)."));

            CheckKit(data, issues);

            if (data.ChaseSpeed < data.PatrolSpeed)
                issues.Add(new Issue(BtIssueSeverity.Warning, "Chase Speed is slower than Patrol Speed: it slows down when it starts a fight."));

            if (data.Voice == null)
                issues.Add(new Issue(BtIssueSeverity.Info, "No Voice: it never barks."));

            return issues;
        }

        // Shared behaviors cast whatever is in the enemy's kit: a slot its tree relies on but the
        // data leaves empty means those branches never fire.
        private static void CheckKit(EnemyData data, List<Issue> issues)
        {
            List<KitNeed> needs = KitNeed.Of(EnemyTreeContents.Nodes(data.BehaviorTree));
            foreach (KitNeed need in KitNeed.Missing(data, needs))
                issues.Add(new Issue(BtIssueSeverity.Warning, $"Its tree uses its kit's {need.Name}, but {need.Field} is empty: those behaviors never fire."));
        }

        private static bool Shoots(EnemyData data, HashSet<Type> nodes) =>
            data.CombatType == EnemyCombatType.Ranged || EnemyTreeContents.UsesAny(nodes, ShootingNodes);

        private static bool MeleeAttacks(EnemyData data, HashSet<Type> nodes) =>
            nodes.Contains(typeof(MeleeAttack)) || (data.CombatType == EnemyCombatType.Melee && data.BehaviorTree == null);
    }
}
