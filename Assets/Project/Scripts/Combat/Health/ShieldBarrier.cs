using System;
using UnityEngine;

namespace CGD.Combat
{
    // A one-way wall of force (a shield-bearer's barrier): shots coming at its front stop on it,
    // so the allies sheltering behind it are covered, while shots travelling the way it faces —
    // fired from behind it, by those allies or its bearer — pass straight through (ShotRay and
    // Projectile ask PassesThrough). It has no health of its own: what it stops is reported
    // through Absorbed, so its owner (a Frontal Shield) can wear it down.
    // A wall that stops every hit (StopEveryHit) also covers those behind it from blasts, zones,
    // melee and beams that come from its front: AreaHits and ShapeHits ask Shelters.
    // A dome (ShieldDome) is the same wall wrapped round a spot: shots coming in from outside stop on
    // it, shots fired from inside it fly out.
    // Keep its collider off the sight and cover layers, so it never blocks anyone's view.
    [DisallowMultipleComponent]
    public class ShieldBarrier : MonoBehaviour, IDamageable
    {
        // The layer barriers are built on: seen by shots (which hit every layer), not by sight and cover checks.
        public const string Layer = "Ignore Raycast";

        private static readonly RaycastHit[] Crossings = new RaycastHit[8];
        private static int _layerMask;

        private static int LayerMaskOfBarriers => _layerMask != 0 ? _layerMask : (_layerMask = LayerMask.GetMask(Layer));

        // Damage it stopped (before any armour: a wall takes the full blow).
        public event Action<float> Absorbed;

        // Metres, or 0 for a flat wall: a dome lets through whatever is fired from inside it.
        private float _domeRadius;
        private bool  _stopsEveryHit;

        public void MakeDome(float radius) => _domeRadius = radius;

        // Not only shots: blasts and blows from its front don't reach those behind it either.
        public void StopEveryHit() => _stopsEveryHit = true;

        public float Armor => 0f;
        public float ArmorReductionPercent { get => 0f; set { } }
        public float MaxHealth => 0f;

        public void TakeDamage(DamageInfo info) => Absorbed?.Invoke(info.RawDamage);

        // True for a shot travelling (roughly) the way a wall faces, or one fired from inside a dome:
        // fired from behind it.
        public bool LetsThrough(Vector3 shotDirection, Vector3 shotOrigin)
        {
            if (_domeRadius > 0f)
            {
                Vector3 inside = shotOrigin - transform.position;
                return inside.sqrMagnitude < _domeRadius * _domeRadius;
            }

            Vector3 forward = transform.forward;
            return forward.x * shotDirection.x + forward.z * shotDirection.z > 0f;
        }

        // Whether a shot from `origin` along `direction` flies on past this collider instead of stopping on it.
        public static bool PassesThrough(Collider collider, Vector3 direction, Vector3 origin) =>
            collider.TryGetComponent(out ShieldBarrier barrier) && barrier.LetsThrough(direction, origin);

        // Whether a raised wall that stops every hit stands between a blast or blow at `from` and a
        // body at `to`, with `from` on its front side: the hit doesn't reach that body.
        public static bool Shelters(Vector3 from, Vector3 to)
        {
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length < 0.01f) return false;

            Vector3 direction = line / length;
            int count = Physics.RaycastNonAlloc(from, direction, Crossings, length, LayerMaskOfBarriers,
                                                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (Crossings[i].collider.TryGetComponent(out ShieldBarrier barrier)
                    && barrier._stopsEveryHit && !barrier.LetsThrough(direction, from))
                    return true;
            }
            return false;
        }
    }
}
