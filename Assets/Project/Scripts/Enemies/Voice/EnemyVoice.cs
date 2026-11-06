using UnityEngine;
using CGD.Audio;

namespace CGD.Enemies
{
    // What one enemy says, from its type's EnemyBarkSet (EnemyData › Voice). It barks on its
    // own when its behavior changes — spotting someone, losing them, flanking, throwing —
    // whichever tree it runs, and on request (Bark node, the director). Every bark goes
    // through the level-wide EnemyBarks.Rules, so a crowd doesn't talk over itself.
    internal sealed class EnemyVoice
    {
        private const float MouthHeight = 1.6f;

        private readonly EnemyAI _ai;
        private EnemyAI.AiState _previous = EnemyAI.AiState.Patrol;
        private float _saidAt = float.NegativeInfinity;

        public EnemyVoice(EnemyAI ai)
        {
            _ai = ai;
            ai.StateChanged += OnStateChanged;
            ai.StepChanged  += OnStepChanged;
            ai.Threw        += OnThrew;
        }

        public bool Say(BarkKind kind)
        {
            EnemyBarkSet set = _ai.Data.Voice;
            if (set == null || _ai.State == EnemyAI.AiState.Dead || !set.TryGet(kind, out BarkLine line)) return false;
            if (Random.value > line.Chance) return false;
            if (!EnemyBarks.Rules.TryClaim(kind, Time.time, ref _saidAt)) return false;

            line.Sound.TryPlay(_ai.transform.position + Vector3.up * MouthHeight);
            EnemyBarks.Report(_ai, kind);
            return true;
        }

        public void Reset()
        {
            _previous = EnemyAI.AiState.Patrol;
            _saidAt   = float.NegativeInfinity;
        }

        private void OnStateChanged(EnemyAI.AiState state)
        {
            BarkKind kind = ForChange(_previous, state, _ai.ReturnIsVictory);
            // A stun is a pause: what it says next compares with what it was doing before.
            if (state != EnemyAI.AiState.Stunned) _previous = state;
            if (kind != BarkKind.None) Say(kind);
        }

        private void OnStepChanged(EngageStep step)
        {
            if (step == EngageStep.Flank)    Say(BarkKind.Flanking);
            if (step == EngageStep.Suppress) Say(BarkKind.Suppressing);
        }

        private void OnThrew() => Say(BarkKind.Grenade);

        // The bark that goes with a change of what it is doing, if any. Picking up where it
        // left off (after a stun) says nothing.
        internal static BarkKind ForChange(EnemyAI.AiState from, EnemyAI.AiState to, bool returnIsVictory) => from == to ? BarkKind.None : to switch
        {
            EnemyAI.AiState.Chase       => BarkKind.Spotted,
            EnemyAI.AiState.Investigate => from is EnemyAI.AiState.Patrol or EnemyAI.AiState.Return ? BarkKind.Investigating : BarkKind.None,
            EnemyAI.AiState.Hunt        => from == EnemyAI.AiState.Chase ? BarkKind.LostTarget : BarkKind.Hunting,
            EnemyAI.AiState.Return      => returnIsVictory ? BarkKind.Victory : BarkKind.NothingFound,
            EnemyAI.AiState.Retreat     => BarkKind.FallingBack,
            EnemyAI.AiState.RaiseAlarm  => BarkKind.RaisingAlarm,
            _                           => BarkKind.None,
        };
    }
}
