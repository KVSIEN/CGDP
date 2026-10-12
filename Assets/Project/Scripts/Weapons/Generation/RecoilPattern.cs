using System;
using UnityEngine;

namespace CGD.Weapons
{
    // How a model's recoil moves sideways while it climbs: which way it leans, whether the
    // drift zig-zags or keeps pushing one way, how jumpy each shot is, how sideways it kicks
    // for its weight and how far it drifts — so an Uzi climbs up and to the right, an AK-47
    // swings side to side and a Vector goes straight up.
    [Serializable]
    public class RecoilPattern
    {
        [Tooltip("Sideways lean of every shot: − left, + right")]
        [Range(-1f, 1f)] public float Bias;
        [Tooltip("Alternating: the drift swings back when it reaches the cap (a zig-zag). OneWay: it keeps pushing toward Bias and holds at the cap")]
        public HorizontalDriftMode Drift = HorizontalDriftMode.Alternating;
        [Tooltip("Shot-to-shot sideways randomness, as a fraction of the horizontal kick (high = jumpy)")]
        [Range(0f, 1f)] public float Jitter = 0.35f;
        [Tooltip("Sideways kick relative to the category's usual share for this much recoil")]
        [Min(0f)] public float Horizontal = 1f;
        [Tooltip("How far the drift goes sideways before it swings back (Alternating) or holds (OneWay), relative to the usual. Low = a tight zig-zag, like an AK-47's")]
        [Min(0.05f)] public float Reach = 1f;
        [Tooltip("How the held gun moves on each shot: 0 drives straight back (a Glock, a Vector), 1 flips the muzzle up (an M1911, a Desert Eagle)")]
        [Range(0f, 1f)] public float MuzzleFlip = 0.5f;
    }
}
