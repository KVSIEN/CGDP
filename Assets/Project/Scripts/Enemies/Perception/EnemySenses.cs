using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;
using CGD.Core;
using CGD.Stealth;

namespace CGD.Enemies
{
    // One enemy's raw senses: whether it can see or hear something right now, and how
    // strongly. Answers only — what the enemy makes of them (awareness, targets, memory) is
    // EnemyPerception's. Sight rays go to the target's chest and head, so cover at chest
    // height hides a crouching target but not a standing one; stealth, smoke and walls apply.
    internal class EnemySenses
    {
        private const float DefaultLoudness = 0.6f;   // footsteps of a hostile without Stealthable

        // Each hostile's body collider and status effects, looked up once instead of per check.
        private static readonly Dictionary<HealthManager, Collider> Bodies = new();
        private static readonly Dictionary<HealthManager, StatusEffectController> Effects = new();
        private static readonly List<ActiveStatus> ActiveBuffer = new();

        private readonly Transform         _self;
        private readonly EnemyData         _data;
        private readonly PerceptionProfile _profile;
        private readonly LayerMask         _obstacleMask;

        public EnemySenses(Transform self, EnemyData data, PerceptionProfile profile, LayerMask obstacleMask)
        {
            _self         = self;
            _data         = data;
            _profile      = profile;
            _obstacleMask = obstacleMask;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Bodies.Clear();
            Effects.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            DestroyedKeys.Remove(Bodies);
            DestroyedKeys.Remove(Effects);
        }

        public Vector3 Eye => _self.position + Vector3.up * EnemyEyes.Height;

        public bool CanSee(HealthManager target, out float gain, out bool instant)
        {
            gain    = 0f;
            instant = false;

            Vector3 eye = Eye;
            AimPoints(target, out Vector3 chest, out Vector3 head);

            Vector3 toChest  = chest - eye;
            float   distance = toChest.magnitude;
            if (distance > _data.SightRange) return false;

            float halfAngle = _data.SightAngle * 0.5f;
            float angle     = Vector3.Angle(_self.forward, toChest);
            if (angle > halfAngle) return false;

            bool hasStealth = Stealthable.TryGet(target, out Stealthable stealth);
            if (hasStealth && !stealth.IsExposedTo(_self.position)) return false;

            if (!IsClear(eye, chest, target.transform) && !IsClear(eye, head, target.transform)) return false;

            instant = distance <= _profile.InstantSightDistance;
            gain    = Mathf.Lerp(_profile.NearSightGain, _profile.FarSightGain, distance / _data.SightRange);
            if (angle > halfAngle * _profile.CentralView) gain *= _profile.PeripheralGain;
            if (hasStealth) gain *= stealth.SightVisibility;
            return true;
        }

        public float FootstepGain(HealthManager target, float distance)
        {
            float loudness = Stealthable.TryGet(target, out Stealthable stealth) ? stealth.FootstepLoudness : DefaultLoudness;
            if (loudness <= 0f) return 0f;

            float range = _data.HearingRadius * loudness;
            if (distance > range || !CanHear(target.transform.position, distance, range, target.transform)) return 0f;

            return _profile.FootstepGain * (1f - 0.5f * distance / range);
        }

        // A bleeding hostile leaves a trail it can follow through walls: like hearing, it gives a
        // rough position and never certainty. Only with a sense of smell (PerceptionProfile).
        public float ScentGain(HealthManager target, float distance)
        {
            float range = _profile.ScentRange;
            if (range <= 0f || distance > range || !IsBleeding(target)) return 0f;

            return _profile.ScentGain * (1f - 0.5f * distance / range);
        }

        private static bool IsBleeding(HealthManager target)
        {
            if (!Effects.TryGetValue(target, out StatusEffectController effects))
            {
                target.TryGetComponent(out effects);
                Effects[target] = effects;
            }
            if (effects == null) return false;

            effects.GetActive(ActiveBuffer);
            foreach (ActiveStatus status in ActiveBuffer)
                if (status.Effect is BleedEffect) return true;
            return false;
        }

        // Within range, and still within it once a wall in between has muffled it. `emitter` is
        // the character making the sound, if any — its own body doesn't muffle it.
        public bool CanHear(Vector3 source, float distance, float range, Transform emitter = null)
        {
            if (distance > range) return false;
            if (distance <= range * _profile.OccludedHearing) return true;

            return IsClear(Eye, source + Vector3.up * 0.5f, emitter);
        }

        // `subject` is the character (or body) the line ends on, which never blocks its own view.
        public bool IsClear(Vector3 from, Vector3 to, Transform subject) =>
            !ConcealmentZone.BlocksLine(from, to) && SightLines.IsClear(from, to, _obstacleMask, subject);

        // Chest and head of the target's body (its collider), so cover at chest height but
        // below the head doesn't hide a standing target — and does hide a crouching one.
        private static void AimPoints(HealthManager target, out Vector3 chest, out Vector3 head)
        {
            if (!Bodies.TryGetValue(target, out Collider body))
            {
                target.TryGetComponent(out body);
                Bodies[target] = body;
            }

            if (body != null && body.enabled)
            {
                Bounds bounds = body.bounds;
                chest = bounds.center;
                head  = new Vector3(bounds.center.x, bounds.max.y - 0.15f, bounds.center.z);
                return;
            }

            Vector3 feet = target.transform.position;
            chest = feet + Vector3.up;
            head  = feet + Vector3.up * 1.6f;
        }
    }
}
