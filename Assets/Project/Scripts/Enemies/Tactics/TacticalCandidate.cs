using UnityEngine;

namespace CGD.Enemies
{
    // One spot a tactical search weighed: where, how it scored, and what became of it.
    public readonly struct TacticalCandidate
    {
        public readonly Vector3          Position;
        public readonly float            Score;
        public readonly CandidateVerdict Verdict;

        public TacticalCandidate(Vector3 position, float score, CandidateVerdict verdict)
        {
            Position = position;
            Score    = score;
            Verdict  = verdict;
        }

        public TacticalCandidate With(CandidateVerdict verdict) => new(Position, Score, verdict);
    }
}
