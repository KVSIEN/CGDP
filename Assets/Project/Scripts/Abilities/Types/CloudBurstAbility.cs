using UnityEngine;
using CGD.Core;

namespace CGD.Abilities
{
    // Bursts a cloud round itself — spores, smoke, ink: the cloud prefab (a smoke cloud, whose
    // concealment hides whoever is inside it) appears at its feet, and everyone close is hit
    // like a shockwave, with its status effects (poison) — usually no damage or push of its own.
    [CreateAssetMenu(fileName = "CloudBurstAbility", menuName = "CGD/Abilities/Cloud Burst")]
    public class CloudBurstAbility : ShockwaveAbility
    {
        [Header("Cloud")]
        [Tooltip("Spawned at its feet (e.g. the smoke grenade's lingering cloud)")]
        public GameObject CloudPrefab;

        public override void Execute(AbilityContext ctx)
        {
            if (CloudPrefab != null) PrefabPool.Spawn(CloudPrefab, ctx.CasterTransform.position, Quaternion.identity);
            base.Execute(ctx);
        }
    }
}
