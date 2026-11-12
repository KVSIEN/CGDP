using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Targeting;

namespace CGD.Enemies
{
    // One faction's director in a level (FactionDoctrine › Overseer): its menace, whether it is
    // holding its members back, its escalation, and the hints it gives. It is the only part of
    // the enemy AI that knows where hostiles really are, which is why it only ever hands out a
    // rough area to check, and only once its members have lost all contact for a while.
    internal sealed class FactionOverseer
    {
        // A target escaping counts towards escalation at most this often.
        private const float EscalationGap = 20f;
        private const float HintSampleRadius = 3f;

        private static readonly List<HealthManager> Hostiles = new();
        private static readonly List<EnemyAI> Calm = new();

        private readonly MenaceGauge _menace;
        private float _lastContactAt;
        private float _nextHintAt;
        private float _nextEscalationAt;

        public FactionOverseer(FactionDoctrine doctrine, Team team, float time)
        {
            Doctrine = doctrine;
            Team     = team;
            OverseerDoctrine style = doctrine.Overseer;
            _menace = new MenaceGauge(style.MenaceRise, style.MenaceDecay, style.MenaceMax,
                                      style.BacksOff, style.BackOffAt, style.BackOffSeconds, style.ResumeBelow);
            _lastContactAt = time;
            _nextHintAt    = time;
        }

        public FactionDoctrine Doctrine { get; }
        public Team            Team     { get; }
        public int   Escalation   { get; private set; }
        public float MenaceShare  => _menace.Share;
        public bool  IsBackingOff => _menace.IsBackingOff;

        private OverseerDoctrine Style => Doctrine.Overseer;

        public bool Watches(EnemyAI enemy) =>
            enemy.isActiveAndEnabled && enemy.State != EnemyAI.AiState.Dead &&
            enemy.Doctrine == Doctrine && enemy.Health.Team == Team;

        public void Tick(float deltaTime, float time)
        {
            int inContact = 0, members = 0;
            var enemies = EnemyRoster.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!Watches(enemies[i])) continue;
                members++;
                if (enemies[i].Perception.Level == AwarenessLevel.Engaged) inContact++;
            }
            if (inContact > 0) _lastContactAt = time;
            _menace.Tick(deltaTime, inContact, time);

            if (members == 0 || inContact > 0 || IsBackingOff) return;
            if (time - _lastContactAt < Style.HintAfterQuiet || time < _nextHintAt) return;

            _nextHintAt = time + Style.HintEvery;
            GiveHint();
        }

        // One of its members gave up a search: the target got away.
        public void OnEscape(float time)
        {
            if (time < _nextEscalationAt) return;
            _nextEscalationAt = time + EscalationGap;
            Escalation = Mathf.Min(Style.MaxEscalation, Escalation + 1);
        }

        // Sends the calm members nearest a hostile to check a rough area around it.
        private void GiveHint()
        {
            Calm.Clear();
            var enemies = EnemyRoster.All;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAI enemy = enemies[i];
                if (Watches(enemy) && enemy.Perception.Level == AwarenessLevel.Unaware && !enemy.Perception.HasLead) Calm.Add(enemy);
            }
            if (Calm.Count == 0 || !TryFindHostile(out HealthManager hostile)) return;

            Vector3 at = hostile.transform.position;
            Calm.Sort((a, b) => (a.transform.position - at).sqrMagnitude.CompareTo((b.transform.position - at).sqrMagnitude));

            float accuracy = Style.HintAccuracy(Escalation);
            int sent = 0;
            for (int i = 0; i < Calm.Count && sent < Style.HintMembers; i++)
            {
                Vector2 offset = Random.insideUnitCircle * accuracy;
                Vector3 area = at + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(area, out NavMeshHit hit, HintSampleRadius, NavMesh.AllAreas)) area = hit.position;
                if (Calm[i].CheckArea(area)) sent++;
            }
        }

        // The hostile nearest to any of its calm members, within hint range.
        private bool TryFindHostile(out HealthManager nearest)
        {
            nearest = null;
            float best = float.MaxValue;
            float range = Style.HintRange;
            foreach (EnemyAI member in Calm)
            {
                var filter = new TargetFilter(TargetAffiliation.Enemies, Team, member.Health, member.TargetMask);
                TargetQuery.Sphere(member.transform.position, range, filter, Hostiles);
                foreach (HealthManager hostile in Hostiles)
                {
                    float distance = (hostile.transform.position - member.transform.position).sqrMagnitude;
                    if (distance >= best) continue;
                    best    = distance;
                    nearest = hostile;
                }
            }
            return nearest != null;
        }
    }
}
