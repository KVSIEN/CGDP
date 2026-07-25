using UnityEngine;

// Required prefab setup: Rigidbody (isKinematic = false, useGravity = true),
// Collider (isTrigger = false) — needs real physics to arc and bounce.
// GrenadeController.Throw() calls Init() right after spawning.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Grenade : MonoBehaviour
{
    private static readonly Collider[] _hitBuffer = new Collider[32];

    private GrenadeData _data;
    private float _fuseTimer;
    private bool _initialized;

    public void Init(GrenadeData data)
    {
        _data = data;
        _fuseTimer = data.FuseTime;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        _fuseTimer -= Time.deltaTime;
        if (_fuseTimer <= 0f) Explode();
    }

    private void Explode()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, _data.ExplosionRadius, _hitBuffer,
            _data.HitMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (!_hitBuffer[i].TryGetComponent<IDamageable>(out var target)) continue;

            float distance = Vector3.Distance(transform.position, _hitBuffer[i].transform.position);
            float falloff  = Mathf.Clamp01(1f - distance / _data.ExplosionRadius);
            if (falloff <= 0f) continue;

            target.TakeDamage(new DamageInfo(_data.ExplosionDamage * falloff, _data.ArmorPenetration, _data.DamageType));
        }

        Destroy(gameObject);
    }
}
