using System;
using UnityEngine;

namespace CGD.Level
{
    // Where a faction's enemies like to start in a room it holds (FactionDesign › Posts): how
    // much likelier each kind of tile is than plain floor, and which kinds make an enemy hold
    // its post (stand guard) rather than patrol. Read by RoomEnemyPlanner.
    [Serializable]
    public class PostPreference
    {
        [Tooltip("How many times likelier than plain floor (1 = no preference)")]
        [SerializeField, Min(0f)] private float _concealed = 1f;
        [SerializeField, Min(0f)] private float _overwatch = 1f;
        [SerializeField, Min(0f)] private float _choke = 1f;
        [SerializeField, Min(0f)] private float _deadEnd = 1f;
        [SerializeField, Min(0f)] private float _route = 1f;
        [Tooltip("At the edge of raised floor, looking down on the floor below (only rooms with elevation have any)")]
        [SerializeField, Min(0f)] private float _highGround = 1f;
        [Tooltip("An enemy starting on one of these holds its post (a guard scanning about) instead of patrolling")]
        [SerializeField] private RoomPostKinds _hold;

        public PostPreference() { }

        public PostPreference(float concealed, float overwatch, float choke, float deadEnd, float route, RoomPostKinds hold,
                              float highGround = 1f)
        {
            _concealed = concealed;
            _overwatch = overwatch;
            _choke     = choke;
            _deadEnd   = deadEnd;
            _route     = route;
            _hold      = hold;
            _highGround = highGround;
        }

        public RoomPostKinds Hold => _hold;

        // The strongest pull among the tile's kinds; plain floor is 1.
        public float WeightOf(RoomPostKinds kinds)
        {
            if (kinds == RoomPostKinds.None) return 1f;
            float weight = 0f;
            if ((kinds & RoomPostKinds.Concealed) != 0) weight = Mathf.Max(weight, _concealed);
            if ((kinds & RoomPostKinds.Overwatch) != 0) weight = Mathf.Max(weight, _overwatch);
            if ((kinds & RoomPostKinds.Choke)     != 0) weight = Mathf.Max(weight, _choke);
            if ((kinds & RoomPostKinds.DeadEnd)   != 0) weight = Mathf.Max(weight, _deadEnd);
            if ((kinds & RoomPostKinds.Route)     != 0) weight = Mathf.Max(weight, _route);
            if ((kinds & RoomPostKinds.HighGround) != 0) weight = Mathf.Max(weight, _highGround);
            return weight;
        }

        public bool Holds(RoomPostKinds kinds) => (kinds & _hold) != 0;

        // The same preference moved toward plain floor: every weight a share of the way back to
        // 1 (strength 1 = unchanged, 0 = no preference). What it holds on stays.
        public PostPreference Toward(float strength)
        {
            float Blend(float weight) => Mathf.Lerp(1f, weight, Mathf.Clamp01(strength));
            return new PostPreference(Blend(_concealed), Blend(_overwatch), Blend(_choke), Blend(_deadEnd), Blend(_route), _hold, Blend(_highGround));
        }
    }
}
