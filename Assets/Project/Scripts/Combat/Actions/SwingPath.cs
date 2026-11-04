namespace CGD.Combat
{
    // Reads a blade's pose off its keys: straight lines between neighbouring keys, held at the
    // first and last key outside them. Keys must be in frame order.
    public static class SwingPath
    {
        public static SwingPose Sample(SwingKey[] keys, float frame)
        {
            if (keys == null || keys.Length == 0) return default;
            if (frame <= keys[0].Frame) return keys[0].Pose;

            for (int i = 1; i < keys.Length; i++)
            {
                if (frame > keys[i].Frame) continue;

                float span = keys[i].Frame - keys[i - 1].Frame;
                float t = span > 0f ? (frame - keys[i - 1].Frame) / span : 1f;
                return SwingPose.Lerp(keys[i - 1].Pose, keys[i].Pose, t);
            }

            return keys[keys.Length - 1].Pose;
        }
    }
}
