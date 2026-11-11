using UnityEngine;

namespace CGD.Enemies
{
    // What an enemy gives away at the two moments that matter, in its faction's manner
    // (FactionDoctrine › Tells): a sign when it first notices something, and a sign — plus a
    // beat before its first attack — when it commits to a fight. Visuals and audio listen
    // for the sign (EnemyAI.Told); the beat is held here (IsCommitting) and respected by
    // its melee swing and ranged bursts.
    internal sealed class EnemyTells
    {
        // Having been fighting this recently, a second look takes this share of the beat.
        private const float RecentFight      = 6f;
        private const float SecondLookScale  = 0.4f;

        private readonly EnemyAI _ai;
        private float _committedUntil = float.NegativeInfinity;
        private float _lastFightEndedAt = float.NegativeInfinity;

        public EnemyTells(EnemyAI ai)
        {
            _ai = ai;
            ai.Perception.LevelChanged += OnLevelChanged;
        }

        // Made up its mind a moment ago and has not acted on it yet.
        public bool IsCommitting => Time.time < _committedUntil;

        public void Reset()
        {
            _committedUntil   = float.NegativeInfinity;
            _lastFightEndedAt = float.NegativeInfinity;
        }

        private void OnLevelChanged(AwarenessLevel from, AwarenessLevel to)
        {
            TellDoctrine tells = _ai.Doctrine.Tells;

            if (to == AwarenessLevel.Engaged)
            {
                float beat = tells.Hesitation.Max > 0f ? tells.Hesitation.EvaluateClamped() : 0f;
                if (Time.time - _lastFightEndedAt < RecentFight) beat *= SecondLookScale;
                _committedUntil = Time.time + beat;
                _ai.NotifyTold(tells.CommittedKind);
            }
            else if (from == AwarenessLevel.Engaged)
            {
                _lastFightEndedAt = Time.time;
            }
            else if (from == AwarenessLevel.Unaware)
            {
                _ai.NotifyTold(tells.NoticedKind);
            }
        }
    }
}
