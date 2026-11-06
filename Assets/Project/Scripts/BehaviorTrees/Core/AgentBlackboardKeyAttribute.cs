using System;

namespace CGD.BehaviorTrees
{
    // Marks a const string naming a key an agent writes to its tree's blackboard itself
    // (e.g. EnemyBlackboard.Health), so the editor offers it and knows it is written.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AgentBlackboardKeyAttribute : Attribute
    {
        public AgentBlackboardKeyAttribute(BlackboardValueType type, string description)
        {
            Type        = type;
            Description = description;
        }

        public BlackboardValueType Type        { get; }
        public string              Description { get; }
    }
}
