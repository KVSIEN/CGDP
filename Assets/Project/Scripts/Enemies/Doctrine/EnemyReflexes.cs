using UnityEngine;
using UnityEngine.AI;
using CGD.Combat;
using CGD.Weapons;

namespace CGD.Enemies
{
    // An enemy getting itself out of the way of a live grenade, in its faction's manner
    // (FactionDoctrine › Reflexes): it has to see the grenade; it reacts after its own beat
    // (a cool head is quick, a slow mind is not); it picks the nearest spot the blast won't
    // reach — outside the radius plus its margin, or behind something solid if it likes cover
    // — that it can walk straight to; and it takes its tree's place until it is clear, like a
    // stun does in reverse. Grenades thrown by its own side don't count.
    internal sealed class EnemyReflexes
    {
        // Between looks for a threat, and the escape spots it weighs.
        private const float ScanEvery      = 0.1f;
        private static readonly float[] Angles    = { 0f, 40f, -40f, 80f, -80f };
        private const float OutwardStep    = 2.5f;
        private const int   OutwardSteps   = 2;
        private const float CoverRingShare = 0.6f;
        private const float RingAngleStep  = 45f;
        private const float SampleRadius   = 1.2f;
        private const float ArrivalDistance = 0.6f;
        // Past the fuse, it gives up waiting for the blast.
        private const float FuseGrace      = 0.6f;

        private readonly EnemyAI _ai;

        private Grenade _threat;
        private float   _nextScan;
        private float   _reactAt = float.PositiveInfinity;
        private bool    _running;
        private float   _endsAt;

        public EnemyReflexes(EnemyAI ai) => _ai = ai;

        private ReflexDoctrine Style => _ai.Doctrine.Reflexes;

        public bool IsActive => _running;

        public void Cancel()
        {
            if (_running) _ai.Agent.isStopped = false;
            _running  = false;
            _threat   = null;
            _reactAt  = float.PositiveInfinity;
        }

        // True while it is dodging a grenade and so not deciding anything else.
        public bool Tick(float deltaTime, float time)
        {
            ReflexDoctrine style = Style;
            if (!style.AvoidsGrenades) return false;

            if (_running) return TickRun(deltaTime, time);

            if (_threat != null && time >= _reactAt) return TryRun(time);
            if (_threat != null && !StillThreatens(_threat)) { _threat = null; _reactAt = float.PositiveInfinity; }
            if (_threat != null || time < _nextScan) return false;

            _nextScan = time + ScanEvery;
            Grenade found = FindThreat();
            if (found == null) return false;

            _threat  = found;
            _reactAt = time + style.ReactionDelay.EvaluateClamped();
            return false;
        }

        // --- Seeing it --------------------------------------------------------------

        private Grenade FindThreat()
        {
            var live = Grenade.Live;
            if (live.Count == 0) return null;

            Vector3 self  = _ai.transform.position;
            float   range = Style.NoticeRange;
            Grenade best  = null;
            float   bestFuse = float.MaxValue;

            for (int i = 0; i < live.Count; i++)
            {
                Grenade grenade = live[i];
                if (grenade == null || !grenade.IsLive || !IsHostile(grenade)) continue;
                if ((grenade.Position - self).sqrMagnitude > range * range) continue;
                if (!grenade.Threatens(self, 0f, _ai.transform) || !CanSee(grenade)) continue;

                if (grenade.FuseLeft >= bestFuse) continue;
                bestFuse = grenade.FuseLeft;
                best = grenade;
            }
            return best;
        }

        private bool StillThreatens(Grenade grenade) =>
            grenade != null && grenade.IsLive && grenade.gameObject.activeInHierarchy &&
            grenade.Threatens(_ai.transform.position, 0f, _ai.transform);

        private bool IsHostile(Grenade grenade) =>
            grenade.ThrowerTeam != Team.None && grenade.ThrowerTeam != _ai.Health.Team;

        private bool CanSee(Grenade grenade)
        {
            Vector3 eye = _ai.transform.position + Vector3.up * EnemyEyes.Height;
            return !Physics.Linecast(eye, grenade.Position, _ai.ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        // --- Getting clear ----------------------------------------------------------

        private bool TryRun(float time)
        {
            Grenade grenade = _threat;
            _threat  = null;
            _reactAt = float.PositiveInfinity;
            if (!StillThreatens(grenade) || !TryFindEscape(grenade, out Vector3 spot)) return false;

            _ai.Agent.isStopped = false;
            if (!_ai.Routing.SetDestination(spot)) return false;

            _running = true;
            _threat  = grenade;
            _endsAt  = time + grenade.FuseLeft + FuseGrace;
            if (Style.Bark != BarkKind.None) _ai.Bark(Style.Bark);
            return true;
        }

        private bool TickRun(float deltaTime, float time)
        {
            NavMeshAgent agent = _ai.Agent;
            bool over = time >= _endsAt || !StillThreatens(_threat)
                        || (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= ArrivalDistance));
            if (over)
            {
                Cancel();
                return false;
            }

            _ai.SetSpeed(_ai.Data.ChaseSpeed * Style.SpeedScale);
            if (Style.WatchesIt) _ai.FaceTowards(_threat.Position, deltaTime);
            return true;
        }

        // The nearest spot it can walk straight to that the blast won't reach.
        private bool TryFindEscape(Grenade grenade, out Vector3 best)
        {
            best = default;
            Vector3 self   = _ai.transform.position;
            Vector3 centre = grenade.Position;
            float   radius = grenade.BlastRadius;
            float   margin = Style.SafetyMargin;

            Vector3 away = self - centre;
            away.y = 0f;
            away = away.sqrMagnitude < 0.01f ? _ai.transform.forward : away.normalized;

            float bestCost = float.MaxValue;
            foreach (float angle in Angles)
            {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * away;
                for (int step = 0; step < OutwardSteps; step++)
                {
                    float distance = Mathf.Max(0.5f, radius + margin) + 0.5f + step * OutwardStep;
                    Consider(grenade, centre + direction * distance, self, ref best, ref bestCost);
                }
            }

            if (Style.SeeksCover)
            {
                // Spots close to the grenade that a wall shields: nearer than running clear of the radius.
                for (float angle = 0f; angle < 360f; angle += RingAngleStep)
                {
                    Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    Consider(grenade, centre + direction * (radius * CoverRingShare), self, ref best, ref bestCost);
                }
            }
            return bestCost < float.MaxValue;
        }

        private void Consider(Grenade grenade, Vector3 spot, Vector3 self, ref Vector3 best, ref float bestCost)
        {
            if (!NavMesh.SamplePosition(spot, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas)) return;
            if (NavMesh.Raycast(self, hit.position, out _, NavMesh.AllAreas)) return;
            if (grenade.Threatens(hit.position, Style.SafetyMargin, _ai.transform)) return;

            float cost = (hit.position - self).sqrMagnitude;
            if (cost >= bestCost) return;
            bestCost = cost;
            best     = hit.position;
        }
    }
}
