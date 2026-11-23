using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A bodyguard's place: picks the ally of its own side it keeps by — the highest tier within
    // reach (a boss, a broodmother, an engineer), ties going to the same one for every guard, or
    // the one nearest the threat, leading the hunt — and keeps between it and the threat, a
    // couple of metres out in front of it, or at its shoulder so it closes in alongside (a pack
    // follower running with its predator), facing the threat. With no threat it stays close to
    // its ward. It keeps a loose station — it only moves once it has drifted well clear of its
    // spot — and keeps the same ward and shoulder from one run to the next, so it doesn't shuffle
    // as the ward, the threat or the node's restarts shift things. Never finishes while the ward
    // lives; fails when there is nobody to guard. Pair it in a Parallel with Shoot Burst.
    [Serializable]
    [BtNodeInfo("Enemy/Tactics/Guard Ally", "Keeps between its ward (highest-tier ally, or the one leading the hunt) and the threat, or at its flank, facing the threat. Runs while the ward lives.")]
    public sealed class GuardAlly : EnemyAction
    {
        [Tooltip("Metres it looks for a ward in")]
        [SerializeField, Min(1f)] private float _searchRadius = 25f;
        [Tooltip("Highest Tier: its most important ally (a bodyguard). Lead Hunter: the ally nearest the threat (a pack follower)")]
        [SerializeField] private GuardWard _ward;
        [Tooltip("In Front: between the ward and the threat. At Flank: at the ward's shoulder, a little behind, closing in alongside it")]
        [SerializeField] private GuardPlace _place;
        [Tooltip("Metres from the ward that it stands: in front of it toward the threat, or out to its side")]
        [SerializeField, Min(0.5f)] private float _standOff = 2.5f;
        [Tooltip("Metres it lets its ward get from it when there is no threat")]
        [SerializeField, Min(1f)] private float _leash = 5f;
        [Tooltip("How loosely it keeps its station: 1 re-plans and sets off after a metre or two of drift, higher lets its ward and the threat move about more before it bothers")]
        [SerializeField, Range(1f, 3f)] private float _slack = 1f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;

        [NonSerialized] private EnemyAI _guarded;
        [Tooltip("Walks the way its faction sneaks (Void keeps to walls, Bio slips through bushes) instead of the shortest way")]
        [SerializeField] private bool _sneak;

        [NonSerialized] private StationKeeper _station;
        [NonSerialized] private bool _following;
        // Which shoulder it keeps to (+1 right, -1 left of the ward facing the threat), picked on
        // entry so it doesn't swap sides as the ward turns.
        [NonSerialized] private float   _side;

        // The lead hunter changes only when another ally is this much nearer the threat.
        private const float SwitchWardMargin = 6f;

        public override string Summary =>
            $"{(_ward == GuardWard.LeadHunter ? "Lead hunter" : "Highest tier")}, {_standOff:0.#} m {(_place == GuardPlace.AtFlank ? "at flank" : "in front")}";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            EnemyAI previous = _guarded;
            _guarded = PickWard(previous);
            if (_guarded != previous) _side = _guarded != null ? SideOf(_guarded.transform.position) : 1f;
            _station ??= new StationKeeper(retarget: 2f * _slack, arrive: 0.8f * _slack, depart: 2f * _slack);
            _station.Reset();
            _following = false;
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (_guarded == null || _guarded.Health.IsDead || !_guarded.isActiveAndEnabled) return BtStatus.Failure;

            Vector3 ward = _guarded.transform.position;
            EnemyPerception perception = Ai.Perception;
            Vector3 spot;
            if (perception.Target != null)
            {
                Vector3 threat = perception.TargetPosition;
                spot = Place(ward, threat);
                Ai.ReportState(EnemyAI.AiState.Chase);
                Ai.ReportStep(EngageStep.Hold);
                Ai.Motion.FaceTowards(threat, context.DeltaTime);
            }
            else
            {
                spot = ward;
                // Falls behind past the leash, then closes right up before it stops again, rather
                // than starting and stopping at the edge of it as the ward shuffles about.
                float gap = Flat(ward - Ai.transform.position).magnitude;
                _following = _following ? gap > _leash * 0.4f : gap > _leash;
                if (!_following)
                {
                    Ai.Routing.Stop();
                    return BtStatus.Running;
                }
            }

            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            _station.Update(spot, Ai.transform.position);
            if (_station.Moving) Ai.Routing.SetDestination(_station.Goal, WalkStyle(_sneak));
            else Ai.Routing.Stop();
            return BtStatus.Running;
        }

        // Where to stand by the ward with a threat about: in front of it toward the threat, or at
        // its shoulder half a stand-off behind.
        private Vector3 Place(Vector3 ward, Vector3 threat)
        {
            Vector3 toThreat = Flat(threat - ward);
            if (toThreat.sqrMagnitude < 0.01f) return ward;
            toThreat.Normalize();

            if (_place == GuardPlace.InFront) return ward + toThreat * _standOff;
            Vector3 right = Vector3.Cross(Vector3.up, toThreat);
            return ward + right * (_side * _standOff) - toThreat * (_standOff * 0.5f);
        }

        // The shoulder it is already nearer, as seen from the ward facing the threat.
        private float SideOf(Vector3 ward)
        {
            Vector3 toThreat = Ai.Perception.Target != null ? Flat(Ai.Perception.TargetPosition - ward) : _guarded.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, toThreat);
            return Vector3.Dot(Flat(Ai.transform.position - ward), right) >= 0f ? 1f : -1f;
        }

        // The ally of its team within reach it keeps by: the highest tier (among equals, the same one
        // for everyone — the lowest instance id — so a pair of guards doesn't end up guarding each
        // other), or the one nearest the threat when it follows the lead hunter and knows a threat.
        private EnemyAI PickWard(EnemyAI current)
        {
            bool byThreat = _ward == GuardWard.LeadHunter && Ai.Perception.Target != null;
            Vector3 threat = Ai.Perception.TargetPosition;
            EnemyAI best = null;
            float bestToThreat = float.MaxValue;
            var all = EnemyRoster.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnemyAI other = all[i];
                if (other == Ai || other.Health == null || other.Health.IsDead || other.Health.Team != Ai.Health.Team) continue;
                if (Flat(other.transform.position - Ai.transform.position).sqrMagnitude > _searchRadius * _searchRadius) continue;
                if (!byThreat)
                {
                    if (best == null || Better(other, best)) best = other;
                    continue;
                }

                float toThreat = Flat(other.transform.position - threat).sqrMagnitude;
                if (toThreat >= bestToThreat) continue;
                bestToThreat = toThreat;
                best         = other;
            }
            return byThreat && StillWorthKeeping(current, best, threat) ? current : best;
        }

        // The ward it already has, unless another is clearly nearer the threat now.
        private bool StillWorthKeeping(EnemyAI current, EnemyAI best, Vector3 threat)
        {
            if (current == null || best == null || current == best || current.Health == null || current.Health.IsDead || !current.isActiveAndEnabled) return false;
            if (Flat(current.transform.position - Ai.transform.position).sqrMagnitude > _searchRadius * _searchRadius) return false;

            float currentToThreat = Flat(current.transform.position - threat).magnitude;
            float bestToThreat    = Flat(best.transform.position - threat).magnitude;
            return currentToThreat - bestToThreat < SwitchWardMargin;
        }

        private static bool Better(EnemyAI a, EnemyAI b)
        {
            int tierA = a.Data != null ? a.Data.Tier : 0;
            int tierB = b.Data != null ? b.Data.Tier : 0;
            return tierA != tierB ? tierA > tierB : a.GetInstanceID() < b.GetInstanceID();
        }

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
