namespace CGD.BehaviorTrees
{
    // A component that runs a behavior tree. The editor finds it on the selected object
    // to show that agent's tree live while playing.
    public interface IBehaviorTreeOwner
    {
        BehaviorTree Tree { get; }
    }
}
