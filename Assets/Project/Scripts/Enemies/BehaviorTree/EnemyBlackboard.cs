using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // What every enemy's tree finds on its blackboard, refreshed each tick before the tree
    // runs — so generic nodes (Compare Number, Score) can read an enemy's state without
    // enemy-specific code. Only what the enemy itself knows: target values are its belief.
    public static class EnemyBlackboard
    {
        public const string Health         = "Health";          // 0..1 of max health
        public const string Morale         = "Morale";          // 0..1, 1 = steady
        public const string Awareness      = "Awareness";       // 0..1, 1 = certain of a hostile
        public const string TargetDistance = "TargetDistance";  // metres to where it believes its target is; unset with none
        public const string Cautious       = "Cautious";        // true while on edge after a search

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
