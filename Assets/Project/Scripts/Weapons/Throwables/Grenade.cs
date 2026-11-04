using System.Collections.Generic;
using UnityEngine;
using CGD.Audio;
using CGD.CameraEffects;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Required prefab setup: Rigidbody (isKinematic = false, useGravity = true),
    // Collider (isTrigger = false) — needs real physics to arc and bounce — and, optionally,
    // the renderers to tint and blink as the fuse burns down.
    // ThrowableController.Throw() calls Init() right after spawning; the thrower's team is
    // immune to the explosion. Everything about the grenade comes from its GrenadeData.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Grenade : MonoBehaviour
    {
        private const float SlowestBlink = 0.5f;   // seconds per blink at the start of the fuse
        private const float FastestBlink = 0.07f;  // … and just before it goes off

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[64];
        private static readonly Dictionary<IDamageable, float> TargetFalloff = new();
        private static readonly HashSet<Rigidbody> Pushed = new();

        [Tooltip("Parts that take the body colour and blink as the fuse burns down")]
        [SerializeField] private Renderer[] _body;

        private Rigidbody _rb;
        private MaterialPropertyBlock _block;
        private GrenadeData _data;
        private DamageSource _source;
        private float _age;
        private bool _initialized;
        private bool _blinking;

        private void Awake()
        {
            _rb    = GetComponent<Rigidbody>();
            _block = new MaterialPropertyBlock();
        }

        public void Init(GrenadeData data, DamageSource source)
        {
            _data = data;
            _source = source;
            _age = 0f;
            _initialized = true;
            _blinking = false;
            Tint(_data.BodyColor);
        }

        private void Update()
        {
            if (!_initialized) return;

            _age += Time.deltaTime;
            if (_age >= _data.FuseTime)
            {
                Explode();
                return;
            }

            bool blink = BlinkOn();
            if (blink == _blinking) return;

            _blinking = blink;
            Tint(blink ? _data.FuseBlinkColor : _data.BodyColor);
        }

        private void OnCollisionEnter()
        {
            if (_initialized && _data.Fuse == FuseMode.OnImpact && _age >= _data.ArmTime) Explode();
        }

        // Blinks faster the closer the fuse is to running out.
        private bool BlinkOn()
        {
            float left = 1f - _age / _data.FuseTime;
            float period = Mathf.Lerp(FastestBlink, SlowestBlink, left);
            return _age % period < period * 0.35f;
        }

        private void Tint(Color color)
        {
            _block.SetColor(BaseColorId, color);
            foreach (Renderer part in _body)
                part.SetPropertyBlock(_block);
        }

        private void Explode()
        {
            Vector3 center = transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, _data.ExplosionRadius, HitBuffer,
                _data.HitMask, QueryTriggerInteraction.Ignore);

            CollectTargets(center, count);
            DamageTargets();
            PushBodies(center, count);

            _data.ExplosionSound.TryPlay(center);
            Noise.Emit(center, _data.NoiseRadius, _source);
            CameraImpulses.Emit(center, _data.ShakeRadius, _data.CameraTrauma);
            if (_data.LingeringZone != null) SpawnZone(center);
            if (_data.ExplosionEffect != null) SpawnFlash(center);

            _initialized = false;
            PrefabPool.Release(gameObject);
        }

        // A target with several hitboxes in range is damaged once, through the one that hurts it most.
        private void CollectTargets(Vector3 center, int count)
        {
            TargetFalloff.Clear();
            for (int i = 0; i < count; i++)
            {
                IDamageable target = Hitbox.FindDamageable(HitBuffer[i]);
                if (target == null) continue;

                Vector3 point = HitBuffer[i].ClosestPoint(center);
                float distance = Vector3.Distance(center, point);
                if (distance > _data.ExplosionRadius) continue;
                if (_data.NeedsLineOfSight && IsShielded(center, point, target)) continue;

                float multiplier = Mathf.Lerp(1f, _data.EdgeDamage, distance / _data.ExplosionRadius);
                if (TargetFalloff.TryGetValue(target, out float best) && best >= multiplier) continue;
                TargetFalloff[target] = multiplier;
            }
        }

        // Something solid between the blast and the target that isn't the target itself.
        private bool IsShielded(Vector3 center, Vector3 point, IDamageable target) =>
            Physics.Linecast(center, point, out RaycastHit hit, _data.BlockerMask, QueryTriggerInteraction.Ignore)
            && Hitbox.FindDamageable(hit.collider) != target;

        // A blast that deals no damage still counts when it applies a status (a stun grenade).
        private bool HasEffects => _data.OnHitEffects != null && _data.OnHitEffects.Length > 0;

        private void DamageTargets()
        {
            foreach (KeyValuePair<IDamageable, float> pair in TargetFalloff)
            {
                float damage = _data.ExplosionDamage * pair.Value;
                if (damage <= 0f && !HasEffects) continue;

                pair.Key.TakeDamage(new DamageInfo(damage, _data.ArmorPenetration, _data.DamageType,
                    source: _source, onHitEffects: _data.OnHitEffects));
            }
        }

        private void PushBodies(Vector3 center, int count)
        {
            if (_data.PushForce <= 0f) return;

            Pushed.Clear();
            for (int i = 0; i < count; i++)
            {
                Rigidbody body = HitBuffer[i].attachedRigidbody;
                if (body == null || body == _rb || body.isKinematic || !Pushed.Add(body)) continue;
                body.AddExplosionForce(_data.PushForce, center, _data.ExplosionRadius, 0.3f, ForceMode.Impulse);
            }
        }

        private void SpawnZone(Vector3 center)
        {
            GameObject zone = PrefabPool.Spawn(_data.LingeringZone, center, Quaternion.identity);
            if (zone.TryGetComponent(out FireZone fire)) fire.Begin(_source, _data);
        }

        private void SpawnFlash(Vector3 center)
        {
            GameObject flash = PrefabPool.Spawn(_data.ExplosionEffect, center, Quaternion.identity);
            if (flash.TryGetComponent(out ExplosionFlash fireball)) fireball.Play(_data.ExplosionRadius, _data.FlashColor);
        }
    }
}
