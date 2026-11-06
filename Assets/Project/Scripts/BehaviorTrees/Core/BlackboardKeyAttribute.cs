using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Marks a node's string field as a blackboard key. The editor draws it with a picker of
    // known keys, and validation warns about keys read that nothing ever writes (usually a
    // typo). Type narrows the picker; leave it out for "any kind of value".
    public sealed class BlackboardKeyAttribute : PropertyAttribute
    {
        public BlackboardKeyAttribute(BlackboardKeyAccess access)
        {
            Access  = access;
            AnyType = true;
        }

        public BlackboardKeyAttribute(BlackboardKeyAccess access, BlackboardValueType type)
        {
            Access = access;
            Type   = type;
        }

        public BlackboardKeyAccess Access  { get; }
        public BlackboardValueType Type    { get; }
        public bool                AnyType { get; }

        // An empty key is a valid choice (Score: a fixed score), not a mistake.
        public bool AllowEmpty { get; set; }
    }
}
