using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // What every enemy's tree finds on its blackboard, refreshed each tick before the tree
    // runs — so generic nodes (Compare Number, Score) can read an enemy's state without
    // enemy-specific code. Only what the enemy itself knows: target values are its belief.
    // The pacing keys come from the director (EnemyPacing): how boldly to fight, never where
    // anyone is.
    public static class EnemyBlackboard
    {
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1 of max health")]
        public const string Health = "Health";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1 of its shield that is gone; 0 when intact or it has none")]
        public const string ShieldMissing = "ShieldMissing";
        [AgentBlackboardKey(BlackboardValueType.Float, "Health and shield lost lately, as a share of both maxima; fades by half every 1.5 s")]
        public const string RecentDamage = "RecentDamage";
        [AgentBlackboardKey(BlackboardValueType.Float, "Share of its remaining health and shield the hostile zones it stands in take each second; 0 when clear")]
        public const string HazardDanger = "HazardDanger";
        [AgentBlackboardKey(BlackboardValueType.Float, "1 for a moment after it is hit, a shot lands close, or it sees a target aim at it; else 0")]
        public const string UnderFire = "UnderFire";
        [AgentBlackboardKey(BlackboardValueType.Float, "Hostiles within 6 m of it")]
        public const string HostilesClose = "HostilesClose";
        [AgentBlackboardKey(BlackboardValueType.Float, "Metres per second its target is moving while sensed; unset with none")]
        public const string TargetSpeed = "TargetSpeed";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1, 1 = steady; low = shaken")]
        public const string Morale = "Morale";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1, 1 = certain of a hostile")]
        public const string Awareness = "Awareness";
        [AgentBlackboardKey(BlackboardValueType.Float, "Metres to where it believes its target is; unset with none")]
        public const string TargetDistance = "TargetDistance";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1 of its target's health and shield when last seen; unset with no target")]
        public const string TargetHealth = "TargetHealth";
        [AgentBlackboardKey(BlackboardValueType.Bool, "True while on edge after a search that found nothing")]
        public const string Cautious = "Cautious";
        [AgentBlackboardKey(BlackboardValueType.Float, "Director's phase: 0 build-up, 1 peak, 2 fade, 3 calm (no director = 0)")]
        public const string Pacing = "Pacing";
        [AgentBlackboardKey(BlackboardValueType.Float, "Director's pressure: 1 neutral, higher when the player is cruising")]
        public const string Pressure = "Pressure";
        [AgentBlackboardKey(BlackboardValueType.Float, "How stressed the player is, 0–1 (director's intensity; 0 without one)")]
        public const string PlayerStress = "PlayerStress";

        internal static void Publish(EnemyAI ai, Blackboard blackboard)
        {
            var health = ai.Health;
            blackboard.SetFloat(Health, health.MaxHealth > 0f ? health.Health / health.MaxHealth : 0f);
            blackboard.SetFloat(ShieldMissing, health.MaxShield > 0f ? 1f - UnityEngine.Mathf.Clamp01(health.Shield / health.MaxShield) : 0f);
            blackboard.SetFloat(RecentDamage, health.RecentDamageShare);
            blackboard.SetFloat(UnderFire, ai.Perception.IsUnderFire ? 1f : 0f);
            float hazard = CGD.Combat.Hazards.DamagePerSecondAt(ai.transform.position, health);
            blackboard.SetFloat(HazardDanger, hazard / UnityEngine.Mathf.Max(1f, health.Health + health.Shield));
            blackboard.SetFloat(HostilesClose, HostilesWithin(ai, CloseRange));
            blackboard.SetFloat(Morale, ai.Morale.Value);
            blackboard.SetFloat(Awareness, ai.Perception.Awareness);
            blackboard.SetBool(Cautious, ai.Perception.IsCautious);

            EnemyPacingState pacing = EnemyPacing.Current;
            blackboard.SetFloat(Pacing, (int)pacing.Phase);
            blackboard.SetFloat(Pressure, pacing.Pressure);
            blackboard.SetFloat(PlayerStress, pacing.Intensity);

            var perception = ai.Perception;
            if (perception.Target != null)
            {
                blackboard.SetFloat(TargetDistance, UnityEngine.Vector3.Distance(ai.transform.position, perception.TargetPosition));
                blackboard.SetFloat(TargetHealth, perception.TargetHealthShare);
                blackboard.SetFloat(TargetSpeed, perception.TargetVelocity.magnitude);
            }
            else
            {
                blackboard.Remove(TargetDistance);
                blackboard.Remove(TargetHealth);
                blackboard.Remove(TargetSpeed);
            }
        }

        private const float CloseRange = 6f;

        private static int HostilesWithin(EnemyAI ai, float range)
        {
            var hostiles = ai.Perception.HostilesAround;
            UnityEngine.Vector3 self = ai.transform.position;
            int count = 0;
            for (int i = 0; i < hostiles.Count; i++)
            {
                var hostile = hostiles[i];
                if (hostile != null && !hostile.IsDead && (hostile.transform.position - self).sqrMagnitude <= range * range) count++;
            }
            return count;
        }
    }
}
