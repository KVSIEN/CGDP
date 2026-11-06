using System;

namespace CGD.BehaviorTrees
{
    // A Selector that tries its children in a fresh random order each time it starts, for
    // variety (pick one of several taunts, idles or flanking routes).
    [Serializable]
    [BtNodeInfo("Composites/Random Selector", "Tries children in a random order until one doesn't fail.")]
    public sealed class RandomSelector : BtComposite
    {
        [NonSerialized] private int[] _order;
        [NonSerialized] private int   _current;

        protected override void OnInitialize(BtContext context) => _order = new int[Children.Count];

        protected override void OnEnter(BtContext context)
        {
            _current = 0;
            for (int i = 0; i < _order.Length; i++) _order[i] = i;

            // Fisher–Yates shuffle.
            for (int i = _order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }
        }

        protected override BtStatus OnTick(BtContext context)
        {
            for (; _current < _order.Length; _current++)
            {
                BtStatus status = Children[_order[_current]].Tick(context);
                if (status != BtStatus.Failure) return status;
            }
            return BtStatus.Failure;
        }
    }
}
