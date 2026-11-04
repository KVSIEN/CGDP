using UnityEngine;

namespace CGD.Combat
{
    // A pose this tick and last tick, so a moving volume can be swept across the space it
    // travelled instead of only checked where it ended up.
    public struct PoseTrail
    {
        public Pose Previous;
        public Pose Current;
        private bool _hasValue;

        // The first pose has no history, so it is its own previous.
        public void Update(Pose pose)
        {
            Previous  = _hasValue ? Current : pose;
            Current   = pose;
            _hasValue = true;
        }

        public void Reset() => _hasValue = false;

        // The pose `fraction` of the way from last tick to this one.
        public Pose Blend(float fraction) => new(
            Vector3.Lerp(Previous.position, Current.position, fraction),
            Quaternion.Slerp(Previous.rotation, Current.rotation, fraction));
    }
}
