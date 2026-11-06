using System;
using System.Collections.Generic;

namespace CGD.BehaviorTrees
{
    // A rolling log of one agent's decisions, for the editor's trace view and breakpoints:
    // which nodes started and how they finished, and what interrupted what. Conditions are
    // only logged when their answer changes — they're re-checked constantly. Recording only
    // happens while a trace is started (BehaviorTree.StartTrace), so it costs nothing
    // otherwise; entries are structs in a fixed ring, so it allocates nothing while running.
    public sealed class BtTrace
    {
        private readonly BtContext      _context;
        private readonly BtTraceEntry[] _entries;
        private readonly Dictionary<BtNode, BtStatus> _conditionAnswers = new();
        private int _next;
        private int _abortDepth;

        internal BtTrace(BtContext context, int capacity)
        {
            _context = context;
            _entries = new BtTraceEntry[Math.Max(16, capacity)];
        }

        // Raised for every entry as it's recorded (breakpoints listen here).
        public event Action<BtTraceEntry> Recorded;

        public int Count { get; private set; }

        // Oldest first.
        public BtTraceEntry this[int index] =>
            _entries[(_next - Count + index + _entries.Length) % _entries.Length];

        public void Clear()
        {
            Count = 0;
            _conditionAnswers.Clear();
        }

        internal void Started(BtNode node)
        {
            if (node is not BtCondition) Add(BtTraceEvent.Started, node, null);
        }

        internal void Finished(BtNode node, BtStatus status)
        {
            if (node is BtCondition)
            {
                if (_conditionAnswers.TryGetValue(node, out BtStatus previous) && previous == status) return;
                _conditionAnswers[node] = status;
            }
            Add(status == BtStatus.Success ? BtTraceEvent.Succeeded : BtTraceEvent.Failed, node, null);
        }

        internal void Interrupted(BtNode stopped, BtNode cause) => Add(BtTraceEvent.Interrupted, stopped, cause);

        // Only the node an abort starts at is logged, not every running node under it — and
        // not when the interruption that caused it was just logged.
        internal void BeginAbort(BtNode node)
        {
            if (_abortDepth++ > 0) return;
            if (Count > 0 && this[Count - 1].Event == BtTraceEvent.Interrupted && this[Count - 1].Node == node) return;
            Add(BtTraceEvent.Aborted, node, null);
        }

        internal void EndAbort() => _abortDepth--;

        private void Add(BtTraceEvent kind, BtNode node, BtNode cause)
        {
            var entry = new BtTraceEntry(_context.Time, _context.TickCount, kind, node, cause);
            _entries[_next] = entry;
            _next = (_next + 1) % _entries.Length;
            if (Count < _entries.Length) Count++;
            Recorded?.Invoke(entry);
        }
    }
}
