using UnityEngine;

namespace CGD.Combat
{
    // The catch rules of an open reflect: until when it is up, how many hits it may still
    // catch, and from which directions. Times are absolute game time.
    public class ReflectWindow
    {
        private float _closesAt = float.NegativeInfinity;
        private int   _catchesLeft; // -1 = unlimited
        private float _arcDeg;

        public bool IsOpen(float now) => now <= _closesAt && _catchesLeft != 0;

        // maxCatches 0 = every hit until the window ends.
        public void Open(float now, float duration, int maxCatches, float arcDeg)
        {
            _closesAt    = now + duration;
            _catchesLeft = maxCatches > 0 ? maxCatches : -1;
            _arcDeg      = arcDeg;
        }

        public void Close() => _closesAt = float.NegativeInfinity;

        // True when the hit is caught, which uses up one catch.
        public bool TryCatch(float now, Vector3 facing, Vector3 toAttacker)
        {
            if (!IsOpen(now) || !FacingArc.Contains(facing, toAttacker, _arcDeg)) return false;
            if (_catchesLeft > 0) _catchesLeft--;
            return true;
        }
    }
}
