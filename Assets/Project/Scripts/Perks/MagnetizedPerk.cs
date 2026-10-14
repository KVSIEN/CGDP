using UnityEngine;

namespace CGD.Perks
{
    // Every shot of the gun it's on resolves instantly along a straight line and punches through
    // whatever it can damage, losing some damage per target, until it meets something solid.
    // Bows and crossbows lose their arc and flight time along with it, so the perk is meant to be
    // rare (see WeightedPerk.MinTier). As a passive it can also carry stat changes, e.g. a drawback.
    [CreateAssetMenu(fileName = "MagnetizedPerk", menuName = "CGD/Perks/Magnetized")]
    public class MagnetizedPerk : PassivePerk
    {
        [Tooltip("Share of the damage a shot keeps after passing through each target (0.75 = the second target takes 75%, the third 56%…)")]
        [SerializeField, Range(0.05f, 1f)] private float _damageKeptPerTarget = 0.75f;

        public float DamageKeptPerTarget => _damageKeptPerTarget;
    }
}
