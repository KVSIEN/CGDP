using UnityEngine;
using CGD.Stealth;

namespace CGD.Enemies
{
    // What an enemy knows about its own cover: whether it is hidden right now (standing in a
    // bush or smoke, or cloaked, and not given away by an attack or a noise) and for how long.
    // Hidden is the stealth system's own verdict, the one hostiles' senses go by — so when this
    // says it is hidden, no hostile outside the same cover can see it.
    internal sealed class EnemyConcealment
    {
        private readonly Stealthable _stealth;

        private bool  _wasHidden;
        private float _hiddenSince;

        public EnemyConcealment(Stealthable stealth) => _stealth = stealth;

        public bool IsHidden => _stealth != null && _stealth.IsHidden;

        // Seconds it has been hidden without a break; 0 when it is not.
        public float HiddenFor => IsHidden ? Time.time - _hiddenSince : 0f;

        public void Tick(float time)
        {
            bool hidden = IsHidden;
            if (hidden && !_wasHidden) _hiddenSince = time;
            _wasHidden = hidden;
        }

        public void Reset() => _wasHidden = false;
    }
}
