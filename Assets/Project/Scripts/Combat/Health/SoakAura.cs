using System.Collections.Generic;
using UnityEngine;

namespace CGD.Combat
{
    // A tank's hold on its team: a share of every hit an ally of its side takes within its radius
    // goes to the tank instead, resolved against the tank's own armour, plating and shield, so the
    // team lives on while the tank soaks. The nearest guardian takes it; status effects stay
    // with the ally that was hit. A guardian never soaks for another guardian, so two tanks
    // can't pass a hit back and forth. Put it on the same object as the HealthManager.
    [DisallowMultipleComponent]
    public class SoakAura : MonoBehaviour
    {
        private static readonly List<SoakAura> Active = new();

        [Tooltip("Metres: allies this close have part of every hit they take go to this character")]
        [SerializeField, Min(0.5f)] private float _radius = 8f;
        [Tooltip("Share of each such hit it takes over")]
        [SerializeField, Range(0f, 0.9f)] private float _share = 0.3f;

        private HealthManager _health;

        public float Radius => _radius;
        public float Share  => _share;

        private void Awake() => TryGetComponent(out _health);

        private void OnEnable()  => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        // Hands the guardian nearest `target` its share of a hit worth `multiplier` times the
        // hit's raw damage, and returns the multiplier the target keeps (1 = no guardian near).
        internal static float Redirect(HealthManager target, in DamageInfo info, float multiplier)
        {
            if (Active.Count == 0 || target.Team == Team.None || target.TryGetComponent(out SoakAura _)) return 1f;

            SoakAura guardian = Nearest(target);
            if (guardian == null) return 1f;

            var soaked = new DamageInfo(info.RawDamage * multiplier * guardian._share, info.ArmorPenetration, info.Type,
                                        source: info.Source, instigator: info.Instigator,
                                        bypassesPlating: info.BypassesPlating, bypassesShield: info.BypassesShield);
            guardian._health.TakeDamage(soaked);
            return 1f - guardian._share;
        }

        private static SoakAura Nearest(HealthManager target)
        {
            Vector3 at = target.transform.position;
            SoakAura best = null;
            float bestSq = float.MaxValue;
            foreach (SoakAura aura in Active)
            {
                HealthManager health = aura._health;
                if (health == null || health.IsDead || health.Team != target.Team || aura._share <= 0f) continue;

                float sq = SquaredFlatDistance(aura.transform.position, at);
                if (sq > aura._radius * aura._radius || sq >= bestSq) continue;
                bestSq = sq;
                best   = aura;
            }
            return best;
        }

        private static float SquaredFlatDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x, z = a.z - b.z;
            return x * x + z * z;
        }
    }
}
