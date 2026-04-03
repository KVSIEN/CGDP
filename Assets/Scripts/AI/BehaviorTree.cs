using System;

public enum BtStatus { Success, Failure, Running }

public abstract class BtNode
{
    public abstract BtStatus Tick();
}

public class BtSelector : BtNode
{
    private readonly BtNode[] _children;
    public BtSelector(params BtNode[] children) => _children = children;

    public override BtStatus Tick()
    {
        foreach (var child in _children)
        {
            var s = child.Tick();
            if (s != BtStatus.Failure) return s;
        }
        return BtStatus.Failure;
    }
}

public class BtSequence : BtNode
{
    private readonly BtNode[] _children;
    public BtSequence(params BtNode[] children) => _children = children;

    public override BtStatus Tick()
    {
        foreach (var child in _children)
        {
            var s = child.Tick();
            if (s != BtStatus.Success) return s;
        }
        return BtStatus.Success;
    }
}

public class BtCondition : BtNode
{
    private readonly Func<bool> _predicate;
    public BtCondition(Func<bool> predicate) => _predicate = predicate;
    public override BtStatus Tick() => _predicate() ? BtStatus.Success : BtStatus.Failure;
}

public class BtAction : BtNode
{
    private readonly Func<BtStatus> _action;
    public BtAction(Func<BtStatus> action) => _action = action;
    public override BtStatus Tick() => _action();
}
