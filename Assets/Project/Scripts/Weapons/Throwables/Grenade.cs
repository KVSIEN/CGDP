using System.Collections.Generic;
using UnityEngine;
using CGD.Audio;
using CGD.CameraEffects;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    // Required prefab setup: Rigidbody (isKinematic = false, useGravity = true, rotation frozen),
    // Collider (isTrigger = false) with a physics material that neither bounces nor grips — the
    // grenade bounces and slows itself from its GrenadeData — and, optionally, the renderers to
    // tint and blink as the fuse burns down.
    // ThrowableController.Throw() calls Init() right after spawning; the thrower's team is
    // immune to the explosion. Everything about the grenade comes from its GrenadeData.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Grenade : MonoBehaviour
    {
        private const float SlowestBlink = 0.5f;   // seconds per blink at the start of the fuse
        private const float FastestBlink = 0.07f;  // … and just before it goes off
        // Shared with GrenadeFlight, which predicts where a throw ends up.
        internal const float GroundNormalY = 0.6f;  // contacts flatter than ~53° count as ground
        internal const float StopSpeed     = 0.3f;  // below this on the ground it comes to rest

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Collider[] HitBuffer = new Collider[64];
        private static readonly Dictionary<IDamageable, float> TargetFalloff = new();
        private static readonly HashSet<Rigidbody> Pushed = new();
        private static readonly List<Grenade> LiveGrenades = new();

        [Tooltip("Parts that take the body colour and blink as the fuse burns down")]
        [SerializeField] private Renderer[] _body;

        private Rigidbody _rb;
        private MaterialPropertyBlock _block;
        private GrenadeData _data;
        private DamageSource _source;
        private Vector3 _incoming; // velocity going into this physics step, before any contact changes it
        private float _age;
        private bool _initialized;
        private bool _blinking;

        // Every armed grenade in play, so those it would hurt can get out of the way.
        public static IReadOnlyList<Grenade> Live => LiveGrenades;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LiveGrenades.Clear();

        public Vector3 Position    => transform.position;
        public bool    IsLive      => _initialized;
        public Team    ThrowerTeam => _source.Team;
        public float   BlastRadius => _data != null ? _data.ExplosionRadius : 0f;
        // Seconds until it goes off (for an impact fuse, the longest it can still fly).
        public float   FuseLeft    => _initialized ? Mathf.Max(0f, _data.FuseTime - _age) : 0f;

        // Whether its blast would reach a point, give or take a margin: inside the radius, and
        // not behind a wall if the blast needs a line. `ignore` is the body that stands there,
        // which does not count as cover from itself.
        public bool Threatens(Vector3 point, float margin, Transform ignore = null)
        {
            if (!_initialized || _data == null) return false;
            if (Vector3.Distance(transform.position, point) > _data.ExplosionRadius + margin) return false;
            if (!_data.NeedsLineOfSight) return true;

            Vector3 chest = point + Vector3.up * 0.9f;
            if (!Physics.Linecast(transform.position, chest, out RaycastHit hit, _data.BlockerMask, QueryTriggerInteraction.Ignore)) return true;
            return ignore != null && hit.collider.transform.IsChildOf(ignore);
        }

        private void OnEnable()
        {
            if (!LiveGrenades.Contains(this)) LiveGrenades.Add(this);
        }

        private void OnDisable() => LiveGrenades.Remove(this);

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
            _incoming = _rb.linearVelocity;
            Tint(_data.BodyColor);
        }

        // The Rigidbody's own gravity covers 1×; the rest of the grenade's gravity is added here.
        private void FixedUpdate()
        {
            if (!_initialized) return;
            _rb.AddForce(Physics.gravity * (_data.GravityScale - 1f), ForceMode.Acceleration);
            _incoming = _rb.linearVelocity;
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

        private void OnCollisionEnter(Collision collision)
        {
            if (!_initialized) return;
            if (_data.Fuse == FuseMode.OnImpact && _age >= _data.ArmTime)
            {
                Explode();
                return;
            }
            if (collision.contactCount > 0) Bounce(collision.GetContact(0).normal);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (!_initialized || collision.contactCount == 0) return;
            if (collision.GetContact(0).normal.y >= GroundNormalY) ApplyGroundDrag();
        }

        // The physics material neither bounces nor grips, so the rebound comes only from the
        // grenade's data and matches what the preview drew. Bouncing from the pre-contact velocity
        // also means a second contact in the same step (a corner) bounces off the first result.
        private void Bounce(Vector3 normal)
        {
            _incoming = _data.BounceVelocity(_incoming, normal);
            _rb.linearVelocity = _incoming;
        }

        // Rolling or sliding on the ground slows it to a stop instead of gliding on.
        private void ApplyGroundDrag()
        {
            Vector3 velocity = _rb.linearVelocity;
            float speed = velocity.magnitude;
            float slowed = speed - _data.GroundDrag * Time.fixedDeltaTime;
            _rb.linearVelocity = slowed <= StopSpeed ? Vector3.zero : velocity * (slowed / speed);
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
            ExplosionFlash.Spawn(_data.ExplosionEffect, center, _data.ExplosionRadius, _data.FlashColor);

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
    }
}
