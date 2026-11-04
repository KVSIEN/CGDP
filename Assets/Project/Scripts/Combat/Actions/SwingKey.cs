using System;

namespace CGD.Combat
{
    // The blade's pose on one frame, counted from the swing event's first frame.
    [Serializable]
    public struct SwingKey
    {
        public int Frame;
        public SwingPose Pose;

        public SwingKey(int frame, SwingPose pose)
        {
            Frame = frame;
            Pose  = pose;
        }
    }
}
