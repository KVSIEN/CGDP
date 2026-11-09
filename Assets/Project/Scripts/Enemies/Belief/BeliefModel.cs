using UnityEngine;

namespace CGD.Enemies
{
    // How likely a spot is to be where an unseen target went, from where it was last seen, which
    // way it was heading and how fast, and how long ago. Pure maths so it can be tested.
    public static class BeliefModel
    {
        // The most ground it is assumed to have covered, whatever the time.
        public const float MaxReach = 40f;
        // Share of its walking speed it is assumed to have kept up while out of sight.
        private const float PaceShare = 0.8f;
        // Weight kept by spots right where it vanished: it may have stopped and ducked behind something.
        private const float StayedShare = 0.15f;

        // 0..1. `heading` is a flat unit vector (zero when unknown).
        public static float Weight(Vector3 spot, Vector3 lastSeen, Vector3 heading, float speed, float elapsed)
        {
            Vector3 offset = spot - lastSeen;
            offset.y = 0f;
            float distance = offset.magnitude;

            float reach  = Mathf.Min(speed * PaceShare * Mathf.Max(0f, elapsed), MaxReach);
            float spread = 3f + 0.35f * reach;
            float off    = distance - reach;
            float band   = Mathf.Exp(-(off * off) / (2f * spread * spread));
            float near   = Mathf.Exp(-distance / 6f);
            float weight = band * (1f - StayedShare) + near * StayedShare;

            // It was going somewhere: spots ahead of it are likelier than those behind.
            if (heading != Vector3.zero && distance > 0.5f)
            {
                float facing = (Vector3.Dot(heading, offset / distance) + 1f) * 0.5f;
                weight *= Mathf.Lerp(0.35f, 1f, facing);
            }
            return weight;
        }
    }
}
