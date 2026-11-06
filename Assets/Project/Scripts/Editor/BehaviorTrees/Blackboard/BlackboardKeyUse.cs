using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // One blackboard key a node reads or writes, with how it uses it.
    internal readonly struct BlackboardKeyUse
    {
        public readonly string                 Key;
        public readonly BlackboardKeyAttribute Attribute;

        public BlackboardKeyUse(string key, BlackboardKeyAttribute attribute)
        {
            Key       = key;
            Attribute = attribute;
        }
    }
}
