using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // A group of enemies placed to fight as one. Put it on a parent object: every EnemyAI under
    // it joins (spawners can add more with Join). Members always coordinate in a fight — they
    // form an encounter together, which hands out roles by their faction's doctrine (see
    // Encounter) — even if their faction does not on its own. On top of that they hear each
    // other's callouts by radio within its range, walls or not, and losing the leader (the
    // first member) shakes everyone's morale.
    public class EnemySquad : MonoBehaviour
    {
        [Tooltip("Metres: callouts reach squad members this far away, through walls (0 = no radio, shouting only)")]
        [SerializeField, Min(0f)] private float _radioRange = 40f;
        [Tooltip("Morale every member loses when the leader goes down")]
        [SerializeField, Range(0f, 1f)] private float _leaderLossMorale = 0.25f;

        private readonly List<EnemyAI> _members = new();
        private EnemyAI _leader;

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
            if (_leader == member) _leader = FirstAlive();
        }

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
                if (member != _leader && IsInPlay(member)) member.Morale.Shake(_leaderLossMorale);
            _leader = FirstAlive();
        }

        private EnemyAI FirstAlive()
        {
            foreach (EnemyAI member in _members)
                if (IsInPlay(member)) return member;
            return null;
        }

        // Agents the spectator has sent away are switched off and may never have woken up (no morale yet).
        private static bool IsInPlay(EnemyAI member) =>
            member != null && member.isActiveAndEnabled && member.State != EnemyAI.AiState.Dead;
    }
}
