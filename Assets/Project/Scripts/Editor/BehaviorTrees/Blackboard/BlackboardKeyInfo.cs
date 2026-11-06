using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // A key the picker can offer: its name, its kind of value (unknown for "any"), where it
    // comes from (the agent, this tree, other trees) and what it means.
    internal readonly struct BlackboardKeyInfo
    {
        public readonly string              Name;
        public readonly BlackboardValueType Type;
        public readonly bool                AnyType;
        public readonly string              Source;
        public readonly string              Description;

        public BlackboardKeyInfo(string name, BlackboardValueType type, bool anyType, string source, string description)
        {
            Name = name; Type = type; AnyType = anyType; Source = source; Description = description;
        }

        public bool Fits(BlackboardKeyAttribute wanted) => wanted.AnyType || AnyType || wanted.Type == Type;
    }
}
