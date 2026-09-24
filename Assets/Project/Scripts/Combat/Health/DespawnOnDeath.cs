using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Removes a character or destructible some time after it dies: returned to its pool
    // when it was spawned from one (so an enemy or crate can be reused), destroyed otherwise.
    [RequireComponent(typeof(HealthManager))]
    public class DespawnOnDeath : MonoBehaviour
    {
        [Tooltip("Seconds the body stays in the world after death")]
        [SerializeField, Min(0f)] private float _delay = 5f;

        private HealthManager _health;

        private void Awake()
        {
            _health = GetComponent<HealthManager>();
            _health.OnDeath += HandleDeath;
        }

        private void OnDestroy() => _health.OnDeath -= HandleDeath;

        private void HandleDeath() => PrefabPool.Release(gameObject, _delay);
    }
}
