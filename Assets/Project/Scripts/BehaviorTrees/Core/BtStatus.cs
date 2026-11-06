namespace CGD.BehaviorTrees
{
    // What a node reports each tick. Running means "ask me again next tick".
    public enum BtStatus
    {
        Running,
        Success,
        Failure,
    }
}
