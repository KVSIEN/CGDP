using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Something in the level an idle enemy may wander over to and spend a while with: grass to
    // graze, a recharging station. Put it on the object, give it a kind (InterestKind: who is
    // drawn, how long they stay, how many fit, what it does for them) and, if the object has
    // particular places to stand, those as spots. Enemies find it through Active, hold a slot
    // while they are there and let it go when they are done or interrupted. UseStarted and
    // UseEnded are for the object itself to react (the grass shrinks, the station lights up).
    public class PointOfInterest : MonoBehaviour
    {
        private static readonly List<PointOfInterest> ActivePoints = new();

        [SerializeField] private InterestKind _kind;
        [Tooltip("Where users stand, one per slot in turn. Empty = spread round this object")]
        [SerializeField] private Transform[] _spots;
        [Tooltip("Metres from the middle that users stand when there are no spots")]
        [SerializeField, Min(0.5f)] private float _spotRadius = 1.5f;

        private EnemyAI[] _holders;

        public static IReadOnlyList<PointOfInterest> Active => ActivePoints;

        public event Action<EnemyAI> UseStarted;
        public event Action<EnemyAI> UseEnded;

        public InterestKind Kind   => _kind;
        public Vector3      Center => transform.position;
        public bool         HasRoom => _kind != null && FreeSlot() >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActivePoints.Clear();

        private void Awake() => _holders = new EnemyAI[_kind != null ? _kind.Capacity : 0];

        // For one added at runtime (a generated room's furniture, PropActivity): its kind and
        // where its users stand.
        internal void Configure(InterestKind kind, Transform[] spots)
        {
            _kind    = kind;
            _spots   = spots;
            _holders = new EnemyAI[kind != null ? kind.Capacity : 0];
        }

        private void OnEnable()  => ActivePoints.Add(this);

        private void OnDisable()
        {
            ActivePoints.Remove(this);
            Array.Clear(_holders, 0, _holders.Length);
        }

        // Holds a slot for the enemy and says where to stand; false when it is full.
        internal bool TryClaim(EnemyAI user, out Vector3 spot)
        {
            spot = Center;
            int slot = Array.IndexOf(_holders, user);
            if (slot < 0) slot = FreeSlot();
            if (slot < 0) return false;

            _holders[slot] = user;
            spot = SpotOf(slot);
            return true;
        }

        internal void Release(EnemyAI user)
        {
            int slot = Array.IndexOf(_holders, user);
            if (slot >= 0) _holders[slot] = null;
        }

        internal void NotifyStarted(EnemyAI user) => UseStarted?.Invoke(user);

        internal void NotifyEnded(EnemyAI user) => UseEnded?.Invoke(user);

        private int FreeSlot()
        {
            if (_holders == null) return -1;
            for (int i = 0; i < _holders.Length; i++)
                if (_holders[i] == null) return i;
            return -1;
        }

        private Vector3 SpotOf(int slot)
        {
            if (_spots != null && _spots.Length > 0 && _spots[slot % _spots.Length] != null) return _spots[slot % _spots.Length].position;
            return InterestRules.SpotAround(Center, slot, _holders.Length, _spotRadius);
        }
    }
}
