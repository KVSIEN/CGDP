using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // A totem left where a curse grenade lands: it stands on the ground in the middle of a lingering
    // zone (the shared DamageZone prefab) that hits everyone hostile inside once a tick, applying
    // the grenade's effects (a Curse stack a second). Anyone can shoot it down: it has its own
    // health, and the zone ends with it. Its caster's side cannot hurt it. Built at runtime
    // like the Shield Dome, so it needs no prefab of its own.
    public sealed class CurseTotem : MonoBehaviour, IDamageable
    {
        private const float Height = 1.4f;

        private DamageSource _source;
        private DamageZone   _zone;
        private float _health;
        private float _maxHealth;
        private float _endsAt;

        public float Armor => 0f;
        public float ArmorReductionPercent { get => 0f; set { } }
        public float MaxHealth => _maxHealth;

        // The zone lasts as long as `zoneSettings.Duration`, or until the totem is destroyed.
        public static CurseTotem Raise(Vector3 centre, float health, Color color, GameObject zonePrefab, ZoneSettings zoneSettings,
            float radius, DamageSource source, LayerMask hitMask, StatusEffectApplication[] effects)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "CurseTotem";
            go.transform.position   = centre + Vector3.up * (Height * 0.5f);
            go.transform.localScale = new Vector3(0.45f, Height * 0.5f, 0.45f);
            go.GetComponent<Renderer>().SetTint(color);

            var totem = go.AddComponent<CurseTotem>();
            totem._source    = source;
            totem._health    = totem._maxHealth = health;
            totem._endsAt    = Time.time + zoneSettings.Duration;

            if (zonePrefab != null)
            {
                GameObject spawned = PrefabPool.Spawn(zonePrefab, centre + Vector3.up * 0.02f, Quaternion.identity);
                if (spawned.TryGetComponent(out DamageZone zone))
                {
                    zone.Begin(zoneSettings, radius, source, hitMask, effects);
                    totem._zone = zone;
                }
            }
            return totem;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (info.Source.Team != Team.None && info.Source.Team == _source.Team) return;

            _health -= info.RawDamage;
            if (_health <= 0f) Fall();
        }

        private void Update()
        {
            if (Time.time >= _endsAt) Fall();
        }

        private void Fall()
        {
            if (_zone != null) _zone.End();
            Destroy(gameObject);
        }
    }
}
