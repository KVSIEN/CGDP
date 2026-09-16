using System.Collections.Generic;
using UnityEngine;

// Required prefab setup: Rigidbody (isKinematic = false, useGravity = true),
// Collider (isTrigger = false) — needs real physics to arc and bounce.
// GrenadeController.Throw() calls Init() right after spawning.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Grenade : MonoBehaviour
{
    private static readonly Collider[] _hitBuffer = new Collider[32];
    private static readonly Dictionary<IDamageable, float> _targetFalloff = new();

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

        // A target with several hitboxes in range is damaged once, using its closest one.
        _targetFalloff.Clear();
        for (int i = 0; i < count; i++)
        {
            IDamageable target = Hitbox.FindDamageable(_hitBuffer[i]);
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, _hitBuffer[i].transform.position);
            float falloff  = Mathf.Clamp01(1f - distance / _data.ExplosionRadius);
            if (_targetFalloff.TryGetValue(target, out float best) && best >= falloff) continue;
            _targetFalloff[target] = falloff;
        }

        foreach (KeyValuePair<IDamageable, float> pair in _targetFalloff)
        {
            if (pair.Value <= 0f) continue;
            pair.Key.TakeDamage(new DamageInfo(_data.ExplosionDamage * pair.Value, _data.ArmorPenetration, _data.DamageType));
        }

        _data.ExplosionSound?.Play(transform.position);
        Destroy(gameObject);
    }
}
