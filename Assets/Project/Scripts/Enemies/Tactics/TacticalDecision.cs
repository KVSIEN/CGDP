using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The most recent search of one kind (TacticalPlan): when it ran, against which threat,
    // the spots it weighed and the one it picked, if any. Rewritten by every search, so it
    // always shows the latest — what the spectator's overlays draw. Kept to a fixed number
    // of spots, reusing its list, so recording costs no allocations.
    public sealed class TacticalDecision
    {
        public const int MaxCandidates = 16;

        private readonly List<TacticalCandidate> _candidates = new(MaxCandidates);

        internal TacticalDecision(TacticalPlan plan) => Plan = plan;

        public TacticalPlan Plan      { get; }
        // Game time it last ran; negative before the first search.
        public float        Time      { get; private set; } = -1f;
        public Vector3      Threat    { get; private set; }
        public bool         Succeeded { get; private set; }
        public Vector3      Chosen    { get; private set; }
        public IReadOnlyList<TacticalCandidate> Candidates => _candidates;

        public bool HasRun => Time >= 0f;

        internal void Begin(Vector3 threat)
        {
            Time      = UnityEngine.Time.time;
            Threat    = threat;
            Succeeded = false;
            _candidates.Clear();
        }

        // Index of the recorded spot, or −1 once the list is full.
        internal int Add(Vector3 position, float score, CandidateVerdict verdict)
        {
            if (_candidates.Count >= MaxCandidates) return -1;
            _candidates.Add(new TacticalCandidate(position, score, verdict));
            return _candidates.Count - 1;
        }

        internal void Mark(int index, CandidateVerdict verdict)
        {
            if (index >= 0 && index < _candidates.Count) _candidates[index] = _candidates[index].With(verdict);
        }

        internal void Pick(Vector3 chosen)
        {
            Succeeded = true;
            Chosen    = chosen;
        }
    }
}
