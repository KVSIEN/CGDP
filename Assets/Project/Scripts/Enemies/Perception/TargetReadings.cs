using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Combat;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Enemies
{
    // An enemy reading its target's body language (BodyLanguage: exposed, watching, flinch).
    // Only while it senses the target: an unseen target gives nothing away. Published to the
    // tree as TargetExposed / TargetWatching / TargetFlinch (EnemyBlackboard).
    internal sealed class TargetReadings
    {
        // Each hostile's gun (only the player carries a WeaponController), looked up once.
        private static readonly Dictionary<HealthManager, WeaponController> Weapons = new();

        private readonly EnemyAI _ai;
        private readonly BodyLanguage _body = new();
        private HealthManager _tracked;

        public TargetReadings(EnemyAI ai) => _ai = ai;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Weapons.Clear();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene scene) => DestroyedKeys.Remove(Weapons);

        public bool  IsExposed => _body.IsExposed;
        public float Watching  => _body.Watching;
        public bool  Flinched  => _body.FlinchedAt(Time.time);

        public void Update(float now)
        {
            EnemyPerception perception = _ai.Perception;
            HealthManager target = perception.Target;
            if (target != _tracked)
            {
                _tracked = target;
                _body.Forget();
            }

            if (target == null || !perception.IsSensing)
            {
                _body.Lose();
                return;
            }

            _body.Read(perception.TargetFacing, _ai.transform.position - target.transform.position, IsReloading(target), now);
        }

        // One of this enemy's decoys was shot: if its target fired, that was a flinch.
        public void NoteDecoyShot(HealthManager shooter)
        {
            if (shooter != null && shooter == _ai.Perception.Target) _body.NoteFlinch(Time.time);
        }

        public void Forget()
        {
            _tracked = null;
            _body.Forget();
        }

        private static bool IsReloading(HealthManager target)
        {
            if (!Weapons.TryGetValue(target, out WeaponController weapon))
            {
                weapon = target.GetComponentInChildren<WeaponController>();
                Weapons[target] = weapon;
            }
            return weapon != null && weapon.IsReloading;
        }
    }
}
