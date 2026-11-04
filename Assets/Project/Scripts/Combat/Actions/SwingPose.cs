using System;
using UnityEngine;

namespace CGD.Combat
{
    // Where a swinging blade points, relative to the aim: Yaw turns it right (+) or left (-),
    // Pitch raises (+) or lowers (-) it, Push slides the whole blade along its own direction
    // (a thrust is two poses that differ only in Push).
    [Serializable]
    public struct SwingPose
    {
        [Range(-180f, 180f)] public float Yaw;
        [Range(-90f, 90f)]   public float Pitch;
        public float Push;

        public SwingPose(float yaw, float pitch = 0f, float push = 0f)
        {
            Yaw   = yaw;
            Pitch = pitch;
            Push  = push;
        }

        public static SwingPose Lerp(SwingPose a, SwingPose b, float t) => new(
            Mathf.Lerp(a.Yaw, b.Yaw, t),
            Mathf.Lerp(a.Pitch, b.Pitch, t),
            Mathf.Lerp(a.Push, b.Push, t));
    }
}
