using UnityEngine;

namespace CGD.UI
{
    // The indicators on screen for hits the player took: one per attacker, each living for a
    // fixed time. A second hit from the same attacker refreshes its indicator instead of adding
    // another; when every slot is taken, the one closest to expiring is replaced. Plain data
    // and timers — the HUD element draws them.
    public sealed class DamageIndicators
    {
        // The last share of an indicator's life over which it fades out.
        private const float FadeShare = 0.4f;

        public struct Slot
        {
            public object  Source;
            public Vector3 Position;
            public float   Remaining;
            public float   Intensity;
        }

        private readonly Slot[] _slots;
        private readonly float  _duration;

        public DamageIndicators(int capacity, float duration)
        {
            _slots    = new Slot[Mathf.Max(1, capacity)];
            _duration = Mathf.Max(0.01f, duration);
        }

        public int Capacity => _slots.Length;

        public Slot this[int index] => _slots[index];

        public bool IsActive(int index) => _slots[index].Remaining > 0f;

        // `intensity` 0..1: how hard the hit was.
        public void Hit(object source, Vector3 position, float intensity)
        {
            int index = FindSource(source);
            bool refreshed = index >= 0;
            if (!refreshed) index = FindFree();
            if (index < 0)   index = FindClosestToExpiring();

            float strongest = refreshed ? Mathf.Max(_slots[index].Intensity, intensity) : intensity;
            _slots[index] = new Slot
            {
                Source    = source,
                Position  = position,
                Remaining = _duration,
                Intensity = Mathf.Clamp01(strongest),
            };
        }

        // Where the attacker is now, for an indicator that follows a moving attacker.
        public void Retarget(int index, Vector3 position) => _slots[index].Position = position;

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Remaining <= 0f) continue;

                _slots[i].Remaining -= deltaTime;
                if (_slots[i].Remaining > 0f) continue;

                _slots[i].Remaining = 0f;
                _slots[i].Source    = null;
            }
        }

        // 1 for most of an indicator's life, easing to 0 over the last part.
        public float Fade(int index) =>
            Mathf.Clamp01(_slots[index].Remaining / (_duration * FadeShare));

        private int FindSource(object source)
        {
            if (source == null) return -1;

            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Remaining > 0f && source.Equals(_slots[i].Source)) return i;
            return -1;
        }

        private int FindFree()
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Remaining <= 0f) return i;
            return -1;
        }

        private int FindClosestToExpiring()
        {
            int oldest = 0;
            for (int i = 1; i < _slots.Length; i++)
                if (_slots[i].Remaining < _slots[oldest].Remaining) oldest = i;
            return oldest;
        }
    }
}
