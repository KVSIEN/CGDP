using System;
using UnityEngine;

namespace CGD.Combat
{
    // A one-way wall of force (a shield-bearer's barrier): shots coming at its front stop on it,
    // so the allies sheltering behind it are covered, while shots travelling the way it faces —
    // fired from behind it, by those allies or its bearer — pass straight through (ShotRay and
    // Projectile ask PassesThrough). It has no health of its own: what it stops is reported
    // through Absorbed, so its owner (a Frontal Shield) can wear it down.
    // Keep its collider off the sight and cover layers, so it never blocks anyone's view.
    [DisallowMultipleComponent]
    public class ShieldBarrier : MonoBehaviour, IDamageable
    {
        // Damage it stopped (before any armour: a wall takes the full blow).
        public event Action<float> Absorbed;

        public float Armor => 0f;
        public float ArmorReductionPercent { get => 0f; set { } }
        public float MaxHealth => 0f;

        public void TakeDamage(DamageInfo info) => Absorbed?.Invoke(info.RawDamage);

        // True for a shot travelling (roughly) the way the barrier faces: fired from behind it.
        public bool LetsThrough(Vector3 shotDirection)
        {
            Vector3 forward = transform.forward;
            return forward.x * shotDirection.x + forward.z * shotDirection.z > 0f;
        }

        // Whether a shot along `direction` flies on past this collider instead of stopping on it.
        public static bool PassesThrough(Collider collider, Vector3 direction) =>
            collider.TryGetComponent(out ShieldBarrier barrier) && barrier.LetsThrough(direction);
    }
}
