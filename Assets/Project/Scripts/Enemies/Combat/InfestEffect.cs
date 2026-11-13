using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // An infestation: hurts like poison while it lasts, and anything that dies with it — an
    // enemy of any side, never the player — bursts into new creatures (the Spawn prefab, an
    // ordinary enemy of its own team) that go looking round the body. Keep a fight with an
    // infector at a distance or it spreads. Applied by hits that carry it (EnemyData › Melee
    // Effects for a biting infector).
    [CreateAssetMenu(fileName = "InfestEffect", menuName = "CGD/Combat/Status Effects/Infest")]
    public class InfestEffect : PoisonEffect
    {
        [Header("Infest")]
        [Tooltip("Enemy prefab that bursts out of whatever dies infested")]
        public GameObject Spawn;
        [Min(1)] public int Count = 2;
        [Tooltip("Game seconds before what burst out dissolves. 0 = it stays")]
        [Min(0f)] public float Lifetime = 30f;

        private const float Scatter = 0.8f;

        // Status effects are cleared on death, so a removal on a dead carrier is the moment to burst.
        public override void OnRemoved(StatusEffectController target)
        {
            if (Spawn == null || target.Damageable is not HealthManager health || !health.IsDead) return;
            if (!health.TryGetComponent(out EnemyHealth _)) return;   // never out of the player

            Vector3 at = health.transform.position;
            for (int i = 0; i < Count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * Scatter;
                GameObject body = PrefabPool.Spawn(Spawn, at + new Vector3(offset.x, 0f, offset.y), Quaternion.Euler(0f, Random.value * 360f, 0f));
                if (body.TryGetComponent(out EnemyAI ai)) ai.AlertTo(at, default);
                if (Lifetime > 0f) PrefabPool.Release(body, Lifetime);
            }
        }
    }
}
