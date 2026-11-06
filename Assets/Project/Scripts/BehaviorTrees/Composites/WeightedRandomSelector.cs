using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A Selector that tries its children in a random order each time it starts, likelier
    // ones first — e.g. flank 1 : charge 3 : taunt 1. Weights follow the children's order
    // (left to right); a child without a weight counts 1, a weight of 0 never runs.
    [Serializable]
    [BtNodeInfo("Composites/Weighted Random Selector", "Tries children in a weighted random order until one doesn't fail.")]
    public sealed class WeightedRandomSelector : BtComposite
    {
        [Tooltip("Chance weight per child, left to right. Missing = 1, 0 = never")]
        [SerializeField] private float[] _weights = Array.Empty<float>();

        [NonSerialized] private int[]   _order;
        [NonSerialized] private float[] _remaining;
        [NonSerialized] private int     _count;
        [NonSerialized] private int     _current;

        public override string Summary => _weights.Length > 0 ? string.Join(" : ", _weights) : "Equal weights";

        protected override void OnInitialize(BtContext context)
        {
            _order     = new int[Children.Count];
            _remaining = new float[Children.Count];
        }

        protected override void OnEnter(BtContext context)
        {
            _current = 0;
            _count   = 0;
            int candidates = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                _remaining[i] = Mathf.Max(0f, i < _weights.Length ? _weights[i] : 1f);
                if (_remaining[i] > 0f) candidates++;
            }

            // Weighted draw without replacement: each pick takes its weight out of the pool.
            for (int n = 0; n < candidates; n++)
            {
                float total = 0f;
                foreach (float weight in _remaining) total += weight;

                float roll = UnityEngine.Random.value * total;
                int   pick = -1;
                for (int i = 0; i < _remaining.Length; i++)
                {
                    if (_remaining[i] <= 0f) continue;
                    pick = i;
                    roll -= _remaining[i];
                    if (roll <= 0f) break;
                }
                _order[_count++] = pick;
                _remaining[pick] = 0f;
            }
        }

        protected override BtStatus OnTick(BtContext context)
        {
            for (; _current < _count; _current++)
            {
                BtStatus status = Children[_order[_current]].Tick(context);
                if (status != BtStatus.Failure) return status;
            }
            return BtStatus.Failure;
        }
    }
}
