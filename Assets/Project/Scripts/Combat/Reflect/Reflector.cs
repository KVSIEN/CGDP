using UnityEngine;
using CGD.Core;
using CGD.Feedback;
using CGD.Stats;

namespace CGD.Combat
{
    // Catches hits while a ReflectProfile is up (opened by a reflect ability) and turns the
    // caught damage into the profile's outputs. A parry can also release a profile for the
    // parried hit (MeleeWeaponData.ParryReflect). Lives on the same object as the
    // HealthManager and runs after a melee guard, so it works on the damage actually taken.
    // Only hits with an attacker are caught, and reflected damage is never caught again.
    public class Reflector : MonoBehaviour, IDamageInterceptor
    {
        private const float ProjectileSpawnOffset = 1f;
        private const float ProjectileLifetime    = 5f;
        // Aim height on attackers without a collider on their root.
        private const float AttackerCentreHeight  = 1f;

        [Tooltip("Where aimed reflections start and point (the player camera). Defaults to this object")]
        [SerializeField] private Transform _aim;

        [Header("Debug")]
        [SerializeField] private bool  _debugDraw     = true;
        [SerializeField] private float _debugDuration = 0.3f;

        private readonly ReflectWindow        _window      = new();
        private readonly ActionTimelineRunner _runner      = new();
        private readonly ActionContext        _timelineCtx = new();

        private ReflectProfile _profile;
        private HealthManager  _health;
        private CharacterStats _stats;
        private DamageSource   _source;

        public int  Order  => 10;
        public bool IsOpen => _profile != null && _window.IsOpen(Time.time);

        private void Awake()
        {
            TryGetComponent(out _health);
            TryGetComponent(out _stats);
            _source = DamageSource.Of(gameObject);
            if (_aim == null) _aim = transform;
        }

        private void OnDisable()
        {
            _window.Close();
            _runner.Stop();
        }

        private void FixedUpdate()
        {
            if (_runner.IsRunning) _runner.Tick();
        }

        // Puts the profile up for its Duration (replacing any reflect already up).
        public void Open(ReflectProfile profile)
        {
            _profile = profile;
            _window.Open(Time.time, profile.Duration, profile.MaxCatches, profile.ArcDeg);
        }

        public float Intercept(in DamageInfo info, float amount, Vector3 point)
        {
            GameObject attacker = info.Source.Owner;
            if (_profile == null || info.IsReflected || attacker == null || amount <= 0f) return amount;
            if (!_window.TryCatch(Time.time, _aim.forward, attacker.transform.position - transform.position)) return amount;

            Release(_profile, info, amount, attacker);
            return amount * (1f - _profile.Negate);
        }

        // Turns `caught` damage from `attacker` into every output of the profile.
        public void Release(ReflectProfile profile, in DamageInfo incoming, float caught, GameObject attacker)
        {
            var reflected = new DamageInfo(caught, incoming.ArmorPenetration, incoming.Type, 1f, _source, isReflected: true);

            ReturnToSender(profile, reflected, attacker);
            LaunchProjectile(profile, reflected, attacker);
            PlayTimeline(profile, caught, attacker);
            Absorb(profile, caught);
            AffectAttacker(profile, reflected, attacker);

            if (!string.IsNullOrEmpty(profile.CatchMessage))
                FeedbackBus.Notify(profile.CatchMessage, NotificationStyle.Success);
        }

        private static void ReturnToSender(ReflectProfile profile, DamageInfo reflected, GameObject attacker)
        {
            if (profile.ReturnShare <= 0f) return;
            IDamageable target = attacker.GetComponentInParent<IDamageable>();
            target?.TakeDamage(reflected.WithDamageScale(profile.ReturnShare));
        }

        private void LaunchProjectile(ReflectProfile profile, DamageInfo reflected, GameObject attacker)
        {
            if (profile.ProjectilePrefab == null || profile.ProjectileShare <= 0f) return;

            Vector3 direction = Direction(profile.ProjectileAim, attacker);
            Vector3 position  = _aim.position + direction * ProjectileSpawnOffset;
            GameObject go = PrefabPool.Spawn(profile.ProjectilePrefab, position, Quaternion.LookRotation(direction));
            if (!go.TryGetComponent(out Projectile projectile)) return;

            projectile.Launch(new ProjectileLaunch
            {
                Damage      = reflected.WithDamageScale(profile.ProjectileShare),
                Velocity    = direction * profile.ProjectileSpeed,
                Lifetime    = ProjectileLifetime,
                MaxDistance = Mathf.Infinity,
                HitMask     = ~0,
                Falloff     = DamageFalloff.None,
            });
        }

        private void PlayTimeline(ReflectProfile profile, float caught, GameObject attacker)
        {
            if (profile.Timeline == null || profile.TimelineShare <= 0f) return;
            if (_runner.IsRunning) _runner.Stop();

            _timelineCtx.Origin        = _aim.position;
            _timelineCtx.Forward       = Direction(profile.TimelineAim, attacker);
            _timelineCtx.Up            = Vector3.up;
            _timelineCtx.SourceRoot    = transform.root;
            _timelineCtx.Source        = _source;
            _timelineCtx.HitMask       = profile.Timeline.HitMask;
            _timelineCtx.HasTarget     = true;
            _timelineCtx.TargetPoint   = CentreOf(attacker);
            _timelineCtx.DamageScale   = caught * profile.TimelineShare;
            _timelineCtx.Reflected     = true;
            _timelineCtx.DebugDraw     = _debugDraw;
            _timelineCtx.DebugDuration = _debugDuration;
            _runner.Begin(profile.Timeline, _timelineCtx);
        }

        private void Absorb(ReflectProfile profile, float caught)
        {
            if (profile.HealShare > 0f && _health != null) _health.Heal(caught * profile.HealShare);
            if (profile.SelfBuff != null && _stats != null) _stats.AddTimed(profile.SelfBuff, profile.SelfBuffDuration);
        }

        private static void AffectAttacker(ReflectProfile profile, DamageInfo reflected, GameObject attacker)
        {
            if (profile.AttackerStun > 0f)
            {
                Stunnable stunnable = attacker.GetComponentInParent<Stunnable>();
                if (stunnable != null) stunnable.ApplyStun(profile.AttackerStun);
            }

            if (profile.AttackerEffects == null || profile.AttackerEffects.Length == 0) return;
            StatusEffectController effects = attacker.GetComponentInParent<StatusEffectController>();
            if (effects == null) return;

            foreach (StatusEffectApplication application in profile.AttackerEffects)
            {
                if (application.Effect != null && Random.value < application.Chance)
                    effects.Apply(application.Effect, reflected);
            }
        }

        private Vector3 Direction(ReflectAim aim, GameObject attacker)
        {
            switch (aim)
            {
                case ReflectAim.ToAttacker:
                    return (CentreOf(attacker) - _aim.position).normalized;

                case ReflectAim.Mirror:
                    Vector3 incoming = (_aim.position - CentreOf(attacker)).normalized;
                    Vector3 normal   = new Vector3(_aim.forward.x, 0f, _aim.forward.z).normalized;
                    return normal == Vector3.zero ? _aim.forward : Vector3.Reflect(incoming, normal);

                default:
                    return _aim.forward;
            }
        }

        private static Vector3 CentreOf(GameObject attacker) =>
            attacker.TryGetComponent(out Collider collider)
                ? collider.bounds.center
                : attacker.transform.position + Vector3.up * AttackerCentreHeight;
    }
}
