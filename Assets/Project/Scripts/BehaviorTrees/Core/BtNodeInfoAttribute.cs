using System;

namespace CGD.BehaviorTrees
{
    // How a node type appears in the behavior tree editor: its place in the create-node
    // menu ("Enemy/Combat/Melee Attack") and a one-line description of what it does.
    // Types without it still work; they're listed under their class name.
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BtNodeInfoAttribute : Attribute
    {
        public BtNodeInfoAttribute(string path, string description)
        {
            Path        = path;
            Description = description;
        }

        public string Path        { get; }
        public string Description { get; }
    }
}
