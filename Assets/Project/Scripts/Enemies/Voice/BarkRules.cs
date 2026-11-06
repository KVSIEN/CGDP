using System;

namespace CGD.Enemies
{
    // Keeps a level's worth of enemies from talking over each other: a short gap after any
    // bark, a longer one before the same kind is said again by anyone, and a rest per speaker.
    // Urgent barks (spotted, grenade, falling back, alarm) skip the any-bark gap — they are
    // the ones the player must hear. Pure rules, so they can be tested.
    public sealed class BarkRules
    {
        private readonly float   _anyGap;
        private readonly float   _sameKindGap;
        private readonly float   _speakerGap;
        private readonly float[] _kindSaidAt;
        private float _anySaidAt = float.NegativeInfinity;

        public BarkRules(float anyGap, float sameKindGap, float speakerGap)
        {
            _anyGap      = anyGap;
            _sameKindGap = sameKindGap;
            _speakerGap  = speakerGap;
            _kindSaidAt  = new float[Enum.GetValues(typeof(BarkKind)).Length];
            Reset();
        }

        public static bool IsUrgent(BarkKind kind) =>
            kind is BarkKind.Spotted or BarkKind.Grenade or BarkKind.FallingBack or BarkKind.RaisingAlarm;

        // Whether the speaker may say it now; if so, records it. speakerSaidAt is the
        // speaker's own last bark time, updated here.
        public bool TryClaim(BarkKind kind, float now, ref float speakerSaidAt)
        {
            if (kind == BarkKind.None) return false;
            if (now - speakerSaidAt < _speakerGap) return false;
            if (now - _kindSaidAt[(int)kind] < _sameKindGap) return false;
            if (!IsUrgent(kind) && now - _anySaidAt < _anyGap) return false;

            speakerSaidAt = _anySaidAt = _kindSaidAt[(int)kind] = now;
            return true;
        }

        public void Reset()
        {
            _anySaidAt = float.NegativeInfinity;
            for (int i = 0; i < _kindSaidAt.Length; i++) _kindSaidAt[i] = float.NegativeInfinity;
        }
    }
}
