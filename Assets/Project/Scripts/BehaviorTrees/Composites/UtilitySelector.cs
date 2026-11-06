using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs whichever child scores best right now (utility AI): each child's score comes
    // from the Score decorators on it — blackboard values such as Health or TargetDistance
    // mapped through curves — and children without one score 0. The next best runs if the
    // best one fails. While a child runs, scores are rechecked now and then and it switches
    // when another child clearly beats the current one.
    [Serializable]
    [BtNodeInfo("Composites/Utility Selector", "Runs the child with the best Score right now; switches when another clearly wins.")]
    public sealed class UtilitySelector : BtComposite
    {
        [Tooltip("Seconds between re-scoring while a child runs. 0 = only when it starts")]
        [SerializeField, Min(0f)] private float _reconsiderEvery = 0.5f;
        [Tooltip("How much better another child must score to interrupt the running one")]
        [SerializeField, Min(0f)] private float _switchMargin = 0.1f;
        [Tooltip("Random amount added to each score, so close calls don't always go the same way")]
        [SerializeField, Min(0f)] private float _randomness = 0.05f;

        [NonSerialized] private int[]   _order;
        [NonSerialized] private float[] _scores;
        [NonSerialized] private int     _rank;
        [NonSerialized] private float   _nextReconsider;

        public override string Summary => _reconsiderEvery > 0f ? $"Re-scores every {_reconsiderEvery:0.##} s" : null;

        protected override void OnInitialize(BtContext context)
        {
            _order  = new int[Children.Count];
            _scores = new float[Children.Count];
        }

        protected override void OnEnter(BtContext context)
        {
            Rank(context);
            _nextReconsider = context.Time + _reconsiderEvery;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_reconsiderEvery > 0f && context.Time >= _nextReconsider) Reconsider(context);

            for (; _rank < _order.Length; _rank++)
            {
                BtStatus status = Children[_order[_rank]].Tick(context);
                if (status != BtStatus.Failure) return status;
            }
            return BtStatus.Failure;
        }

        private void Reconsider(BtContext context)
        {
            _nextReconsider = context.Time + _reconsiderEvery;
            if (_rank >= _order.Length) return;

            int   running      = _order[_rank];
            float runningScore = _scores[running];
            Rank(context);

            if (_order[0] == running || _scores[_order[0]] <= runningScore + _switchMargin)
            {
                // Keep going with the one that's running.
                _rank = Array.IndexOf(_order, running);
                return;
            }
            Children[running].Abort(context);
        }

        // Scores every child and orders them best first (insertion sort: few children).
        private void Rank(BtContext context)
        {
            _rank = 0;
            for (int i = 0; i < Children.Count; i++)
            {
                _scores[i] = ScoreOf(Children[i], context) + UnityEngine.Random.value * _randomness;
                int at = i;
                while (at > 0 && _scores[_order[at - 1]] < _scores[i])
                {
                    _order[at] = _order[at - 1];
                    at--;
                }
                _order[at] = i;
            }
        }

        private static float ScoreOf(BtNode child, BtContext context) => child is Score score ? score.Evaluate(context) : 0f;
    }
}
