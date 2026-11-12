using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // Kin of one faction (same doctrine, same team) fighting the same target, as a group: a
    // lead by the faction's succession rule, a role for each member from the faction's table,
    // and a posture. When the lead falls the successor steps up — its nerve steadies, it takes
    // the lead's job and gives a sign — and the group may regroup on its second table for a
    // while. Its overseer backing off also puts it in regroup. A lone member gets no role, so
    // it fights as it would on its own. Rebuilt every round by EncounterBoard.
    internal sealed class Encounter
    {
        private const int MinMembersForRoles = 2;

        private readonly List<EnemyAI> _members = new();
        private readonly HashSet<EnemyAI> _seen = new();
        private readonly Dictionary<EnemyAI, float> _joinedAt = new();
        private readonly Dictionary<EnemyAI, SquadRole> _roles = new();

        private RoleCandidate[] _candidates = new RoleCandidate[4];
        private SquadRole[]     _assigned   = new SquadRole[4];
        private bool[]          _taken      = new bool[4];

        private float   _regroupUntil;
        private EnemyAI _steppedUp;
        private float   _steppedUpUntil;
        private int     _nextFlankSide = 1;

        public Encounter(FactionDoctrine doctrine, Team team, HealthManager target)
        {
            Doctrine = doctrine;
            Team     = team;
            Target   = target;
        }

        public FactionDoctrine Doctrine { get; }
        public Team            Team     { get; }
        public HealthManager   Target   { get; }
        public EnemyAI         Lead     { get; private set; }
        public EncounterPosture Posture { get; private set; }
        public IReadOnlyList<EnemyAI> Members => _members;
        public bool IsEmpty => _members.Count == 0;

        public bool Matches(FactionDoctrine doctrine, Team team, HealthManager target) =>
            Doctrine == doctrine && Team == team && Target == target;

        public SquadRole RoleOf(EnemyAI member) => _roles.TryGetValue(member, out SquadRole role) ? role : SquadRole.None;
        public bool IsSteppedUp(EnemyAI member) => member == _steppedUp && Time.time < _steppedUpUntil;

        // Flankers take turns going left and right, so two of them close in from both sides.
        public int NextFlankSide()
        {
            _nextFlankSide = -_nextFlankSide;
            return _nextFlankSide;
        }

        // --- A round --------------------------------------------------------------

        public void BeginRound() => _seen.Clear();

        public void Mark(EnemyAI member, float time)
        {
            _seen.Add(member);
            if (_members.Contains(member)) return;

            _members.Add(member);
            _joinedAt[member] = time;
            member.JoinEncounter(this);
        }

        // Members not seen this round have left (or fallen); the lead is replaced, roles handed out.
        public void EndRound(float time, bool overseerBackingOff)
        {
            bool leadFell = false;
            for (int i = _members.Count - 1; i >= 0; i--)
            {
                EnemyAI member = _members[i];
                if (_seen.Contains(member)) continue;

                if (member == Lead) leadFell = IsDown(member);
                Remove(i);
            }
            if (_members.Count == 0)
            {
                Lead = null;
                return;
            }

            if (Posture == EncounterPosture.Regroup && time >= _regroupUntil) Posture = EncounterPosture.Steady;

            Gather(time);
            if (Lead == null || !_members.Contains(Lead)) ChooseLead(time, leadFell);

            EncounterPosture posture = overseerBackingOff ? EncounterPosture.Regroup : Posture;
            AssignRoles(posture);
        }

        // Everyone leaves: the encounter is over.
        public void Dissolve()
        {
            for (int i = _members.Count - 1; i >= 0; i--) Remove(i);
            Lead = null;
        }

        private void Remove(int index)
        {
            EnemyAI member = _members[index];
            _members.RemoveAt(index);
            _joinedAt.Remove(member);
            _roles.Remove(member);
            if (member != null) member.LeaveEncounter(this);
        }

        // A member that died, or was despawned, rather than one that simply lost the target.
        private static bool IsDown(EnemyAI member) =>
            member == null || !member.isActiveAndEnabled || member.State == EnemyAI.AiState.Dead;

        // --- Lead and roles ---------------------------------------------------------

        private void ChooseLead(float time, bool leadFell)
        {
            int index = Succession.PickLead(_candidates, _members.Count, Doctrine.Encounter.Succession);
            Lead = index >= 0 ? _members[index] : null;
            if (Lead == null) return;
            _candidates[index] = Candidate(Lead, time, isLead: true);
            if (!leadFell) return;

            // The lead fell: its successor steps up, and the group may shift posture.
            EncounterDoctrine style = Doctrine.Encounter;
            if (style.OnLeadLost == EncounterPosture.Regroup)
            {
                Posture       = EncounterPosture.Regroup;
                _regroupUntil = time + style.RegroupSeconds;
            }
            if (style.SteppedUpSeconds <= 0f) return;

            _steppedUp      = Lead;
            _steppedUpUntil = time + style.SteppedUpSeconds;
            Lead.Morale.Steady(style.SuccessorNerve);
            Lead.NotifyTold(TellKind.StepUp);
        }

        private void Gather(float time)
        {
            int count = _members.Count;
            if (_candidates.Length < count)
            {
                _candidates = new RoleCandidate[count * 2];
                _assigned   = new SquadRole[count * 2];
                _taken      = new bool[count * 2];
            }
            for (int i = 0; i < count; i++) _candidates[i] = Candidate(_members[i], time, _members[i] == Lead);
        }

        private RoleCandidate Candidate(EnemyAI member, float time, bool isLead)
        {
            var perception = member.Perception;
            var health     = member.Health;
            Vector3 targetPosition = perception.TargetPosition;
            Vector3 fromTarget = member.transform.position - targetPosition;
            fromTarget.y = 0f;

            // 0 in front of where the target was facing, 1 behind it.
            Vector3 facing = perception.TargetFacing;
            float offAxis = facing.sqrMagnitude > 0.01f && fromTarget.sqrMagnitude > 0.01f
                ? (1f - Vector3.Dot(facing, fromTarget.normalized)) * 0.5f
                : 0.5f;

            return new RoleCandidate(
                isRanged:    member.Data.CombatType == EnemyCombatType.Ranged,
                distance:    fromTarget.magnitude,
                offAxis:     offAxis,
                healthShare: health.MaxHealth > 0f ? health.Health / health.MaxHealth : 0f,
                shieldShare: health.MaxShield > 0f ? health.Shield / health.MaxShield : 0f,
                tier:        member.Data.Tier,
                inCover:     member.Tactics.IsInCover(targetPosition),
                seniority:   _joinedAt.TryGetValue(member, out float joined) ? time - joined : 0f,
                isLead:      isLead,
                currentRole: RoleOf(member));
        }

        private void AssignRoles(EncounterPosture posture)
        {
            _roles.Clear();
            int count = _members.Count;
            if (count < MinMembersForRoles) return;

            EncounterDoctrine style = Doctrine.Encounter;
            RoleAssignment.Assign(_candidates, count, style.RolesFor(posture), style.FallbackFor(posture), _assigned, _taken);
            for (int i = 0; i < count; i++) _roles[_members[i]] = _assigned[i];
        }
    }
}
