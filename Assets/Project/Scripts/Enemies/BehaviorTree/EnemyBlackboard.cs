using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // What every enemy's tree finds on its blackboard, refreshed each tick before the tree
    // runs — so generic nodes (Compare Number, Score) can read an enemy's state without
    // enemy-specific code. Only what the enemy itself knows: target values are its belief.
    public static class EnemyBlackboard
    {
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1 of max health")]
        public const string Health = "Health";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1, 1 = steady; low = shaken")]
        public const string Morale = "Morale";
        [AgentBlackboardKey(BlackboardValueType.Float, "0–1, 1 = certain of a hostile")]
        public const string Awareness = "Awareness";
        [AgentBlackboardKey(BlackboardValueType.Float, "Metres to where it believes its target is; unset with none")]
        public const string TargetDistance = "TargetDistance";
        [AgentBlackboardKey(BlackboardValueType.Bool, "True while on edge after a search that found nothing")]
        public const string Cautious = "Cautious";

        internal static void Publish(EnemyAI ai, Blackboard blackboard)
        {
            var health = ai.Health;
            blackboard.SetFloat(Health, health.MaxHealth > 0f ? health.Health / health.MaxHealth : 0f);
            blackboard.SetFloat(Morale, ai.Morale.Value);
            blackboard.SetFloat(Awareness, ai.Perception.Awareness);
            blackboard.SetBool(Cautious, ai.Perception.IsCautious);

            var perception = ai.Perception;
            if (perception.Target != null)
                blackboard.SetFloat(TargetDistance, UnityEngine.Vector3.Distance(ai.transform.position, perception.TargetPosition));
            else
                blackboard.Remove(TargetDistance);
        }
    }
}
