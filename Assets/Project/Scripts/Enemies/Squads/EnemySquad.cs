using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // A group of enemies that fight as one. Put it on a parent object: every EnemyAI under it
    // joins (spawners can add more with Join). While members fight, the squad hands out roles
    // (SquadRoles: some pin the target down or go straight in, the rest flank — from opposite
    // sides) and members' behavior follows them. Members also hear each other's callouts by
    // radio within its range, walls or not, and losing the leader (the first member) shakes
    // everyone's morale.
    public class EnemySquad : MonoBehaviour
    {
        [Tooltip("Metres: callouts reach squad members this far away, through walls (0 = no radio, shouting only)")]
        [SerializeField, Min(0f)] private float _radioRange = 40f;
        [Tooltip("Ranged members that keep the target pinned down; the other ranged members flank")]
        [SerializeField, Min(0)] private int _suppressors = 1;
        [Tooltip("Melee members that go straight in; the other melee members flank")]
        [SerializeField, Min(0)] private int _assaulters = 2;
        [Tooltip("Seconds between role hand-outs while fighting")]
        [SerializeField, Min(0.2f)] private float _reassignEvery = 2f;
        [Tooltip("Morale every member loses when the leader goes down")]
        [SerializeField, Range(0f, 1f)] private float _leaderLossMorale = 0.25f;

        private readonly List<EnemyAI> _members = new();
        private readonly Dictionary<EnemyAI, SquadRole> _roles = new();
        private EnemyAI _leader;
        private float   _nextAssign;
        private int     _nextFlankSide = 1;

        public IReadOnlyList<EnemyAI> Members => _members;
        public EnemyAI Leader => _leader;

        private void Awake()
        {
            foreach (EnemyAI member in GetComponentsInChildren<EnemyAI>(true)) Join(member);
        }

        private void OnEnable()
        {
            EnemyCallouts.Raised += Relay;
            FallenAllies.Fell    += OnFell;
        }

        private void OnDisable()
        {
            EnemyCallouts.Raised -= Relay;
            FallenAllies.Fell    -= OnFell;
        }

        public void Join(EnemyAI member)
        {
            if (member == null || _members.Contains(member)) return;
            _members.Add(member);
            member.JoinSquad(this);
            if (_leader == null) _leader = member;
        }

        public void Leave(EnemyAI member)
        {
            _members.Remove(member);
            _roles.Remove(member);
            if (_leader == member) _leader = FirstAlive();
        }

        public SquadRole RoleOf(EnemyAI member) => _roles.TryGetValue(member, out SquadRole role) ? role : SquadRole.None;

        // Flankers take turns going left and right, so two of them close in from both sides.
        public int NextFlankSide()
        {
            _nextFlankSide = -_nextFlankSide;
            return _nextFlankSide;
        }

        private void Update()
        {
            if (Time.time < _nextAssign) return;
            _nextAssign = Time.time + _reassignEvery;
            AssignRoles();
        }

        // Members currently fighting (or hunting someone they just lost) get roles; the rest none.
        private void AssignRoles()
        {
            _roles.Clear();
            var fighting = new List<EnemyAI>();
            foreach (EnemyAI member in _members)
                if (IsFighting(member)) fighting.Add(member);
            if (fighting.Count == 0) return;

            var ranged   = new bool[fighting.Count];
            var distance = new float[fighting.Count];
            var roles    = new SquadRole[fighting.Count];
            for (int i = 0; i < fighting.Count; i++)
            {
                EnemyAI member = fighting[i];
                ranged[i]   = member.Data.CombatType == EnemyCombatType.Ranged;
                distance[i] = Vector3.Distance(member.transform.position, member.Perception.TargetPosition);
            }

            SquadRoles.Assign(ranged, distance, _suppressors, _assaulters, roles);
            for (int i = 0; i < fighting.Count; i++) _roles[fighting[i]] = roles[i];
        }

        private static bool IsFighting(EnemyAI member) =>
            member != null && member.isActiveAndEnabled && member.State != EnemyAI.AiState.Dead &&
            member.Perception.Target != null &&
            member.Perception.Level is AwarenessLevel.Engaged or AwarenessLevel.Searching;

        // A member's callout reaches every other member in radio range, walls or not.
        private void Relay(EnemyCallout callout)
        {
            if (_radioRange <= 0f || callout.ThroughWalls || !_members.Contains(callout.Caller)) return;

            var radio = new EnemyCallout(callout.Caller, callout.From, callout.Reported, callout.Team, _radioRange, throughWalls: true);
            foreach (EnemyAI member in _members)
                if (member != callout.Caller && member.isActiveAndEnabled) member.Perception.OnCallout(radio);
        }

        private void OnFell(FallenAlly fallen)
        {
            if (fallen.Body != _leader) return;

            foreach (EnemyAI member in _members)
                if (member != _leader && member.State != EnemyAI.AiState.Dead) member.Morale.Shake(_leaderLossMorale);
            _leader = FirstAlive();
        }

        private EnemyAI FirstAlive()
        {
            foreach (EnemyAI member in _members)
                if (member != null && member.State != EnemyAI.AiState.Dead) return member;
            return null;
        }
    }
}
