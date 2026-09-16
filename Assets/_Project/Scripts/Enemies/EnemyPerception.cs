using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // What an enemy knows about hostile characters: the current target, whether it is
    // detected right now (seen, or within proximity range), and the last position worth
    // investigating. Noise and incoming hits create "leads" the AI can follow up on.
    public class EnemyPerception
    {
        private const float EyeHeight = 1.5f;

        private static readonly Collider[] _hitBuffer = new Collider[32];

        private readonly Transform _self;
        private readonly EnemyData _data;
        private readonly LayerMask _targetMask;
        private readonly LayerMask _obstacleMask;
        private readonly Team      _team;
        private bool _hasLead;

        public HealthManager Target            { get; private set; }
        public bool          IsTargetDetected  { get; private set; }
        public Vector3       LastKnownPosition { get; private set; }

        public EnemyPerception(Transform self, EnemyData data, LayerMask targetMask, LayerMask obstacleMask, Team team)
        {
            _self             = self;
            _data             = data;
            _targetMask       = targetMask;
            _obstacleMask     = obstacleMask;
            _team             = team;
            LastKnownPosition = self.position;
        }

        public void Tick()
        {
            if (Target != null && Target.IsDead) Target = null;
            if (Target == null) Target = FindDetectableHostile();

            IsTargetDetected = Target != null && CanDetect(Target);
            if (IsTargetDetected) LastKnownPosition = Target.transform.position;
        }

        // True once per new lead (noise heard, hit taken) since the last call.
        public bool ConsumeLead()
        {
            bool had = _hasLead;
            _hasLead = false;
            return had;
        }

        public void LoseTarget()
        {
            Target           = null;
            IsTargetDetected = false;
        }

        public void OnNoise(NoiseEvent noise)
        {
            if (!IsHostile(noise.Source)) return;
            if ((noise.Position - _self.position).sqrMagnitude > noise.Radius * noise.Radius) return;

            Report(noise.Position, noise.Source);
        }

        public void OnHit(DamageInfo hit)
        {
            if (!IsHostile(hit.Source) || hit.Source.Owner == null) return;

            Report(hit.Source.Owner.transform.position, hit.Source);
        }

        private void Report(Vector3 position, DamageSource source)
        {
            LastKnownPosition = position;
            _hasLead = true;

            if (Target == null && source.Owner != null)
                Target = source.Owner.GetComponentInParent<HealthManager>();
        }

        private bool IsHostile(DamageSource source) => source.Team != Team.None && source.Team != _team;

        private HealthManager FindDetectableHostile()
        {
            int count = Physics.OverlapSphereNonAlloc(_self.position, _data.SightRange, _hitBuffer,
                _targetMask, QueryTriggerInteraction.Ignore);

            HealthManager nearest = null;
            float nearestSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (Hitbox.FindDamageable(_hitBuffer[i]) is not HealthManager candidate) continue;
                if (candidate.Team == _team || candidate.IsDead || !CanDetect(candidate)) continue;

                float sqrDist = (candidate.transform.position - _self.position).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private bool CanDetect(HealthManager target) => IsWithinProximity(target) || CanSee(target);

        private bool IsWithinProximity(HealthManager target) =>
            (target.transform.position - _self.position).sqrMagnitude <= _data.HearingRadius * _data.HearingRadius;

        private bool CanSee(HealthManager target)
        {
            Vector3 eyePos   = _self.position + Vector3.up * EyeHeight;
            Vector3 toTarget = target.transform.position - eyePos;
            float   dist     = toTarget.magnitude;

            if (dist > _data.SightRange) return false;
            if (Vector3.Angle(_self.forward, toTarget) > _data.SightAngle * 0.5f) return false;

            return !Physics.Raycast(eyePos, toTarget / dist, dist, _obstacleMask, QueryTriggerInteraction.Ignore);
        }
    }
}
