using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;
using CGD.Combat;
using CGD.Core;

namespace CGD.Enemies
{
    // The minions an enemy has called up (Phase Shift): spawned from the pool round a point and
    // sent hunting for its hostiles, then released once their time is up, a while after they
    // fall, or as soon as their summoner does. They are ordinary enemies of the summoner's team.
    internal sealed class EnemyMinions : IMinionSummoner
    {
        private const float ScatterRadius = 1.2f;
        private const float SnapDistance  = 3f;
        private const float DeadLinger    = 2f;

        private struct Minion
        {
            public GameObject  Body;
            public EnemyHealth Health;
            public float       ExpiresAt;
            public float       DiedAt;
        }

        private readonly EnemyAI _ai;
        private readonly List<Minion> _minions = new();

        public EnemyMinions(EnemyAI ai) => _ai = ai;

        public int Alive => _minions.Count;

        public void Summon(GameObject prefab, int count, Vector3 around, float lifetime)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                float angle = (i + Random.value) / Mathf.Max(1, count) * Mathf.PI * 2f;
                Vector3 spot = around + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ScatterRadius;
                if (NavMesh.SamplePosition(spot, out NavMeshHit hit, SnapDistance, NavMesh.AllAreas)) spot = hit.position;

                GameObject body = PrefabPool.Spawn(prefab, spot, Quaternion.Euler(0f, Random.value * 360f, 0f));
                if (!body.TryGetComponent(out EnemyHealth health)) continue;

                if (body.TryGetComponent(out ISummoned summoned)) summoned.SummonedBy(_ai);
                SendHunting(body);
                _minions.Add(new Minion { Body = body, Health = health, ExpiresAt = Time.time + lifetime, DiedAt = -1f });
            }
        }

        public void Tick(float now)
        {
            for (int i = _minions.Count - 1; i >= 0; i--)
            {
                Minion minion = _minions[i];
                if (minion.Health.IsDead && minion.DiedAt < 0f)
                {
                    minion.DiedAt = now;
                    _minions[i] = minion;
                }

                bool gone = minion.Body == null || !minion.Body.activeInHierarchy;
                bool spent = now >= minion.ExpiresAt || (minion.DiedAt >= 0f && now - minion.DiedAt >= DeadLinger);
                if (!gone && !spent) continue;

                if (!gone) PrefabPool.Release(minion.Body);
                _minions.RemoveAt(i);
            }
        }

        // The summoner fell (or was revived): what it called up goes with it.
        public void DismissAll()
        {
            foreach (Minion minion in _minions)
                if (minion.Body != null && minion.Body.activeInHierarchy) PrefabPool.Release(minion.Body);
            _minions.Clear();
        }

        // Straight to where the summoner's quarry is (or last was), so they hunt rather than wait to notice.
        private void SendHunting(GameObject body)
        {
            if (!body.TryGetComponent(out EnemyAI minion) || _ai.Perception.Target == null) return;

            minion.AlertTo(_ai.Perception.TargetPosition, DamageSource.Of(_ai.gameObject));
        }
    }
}
