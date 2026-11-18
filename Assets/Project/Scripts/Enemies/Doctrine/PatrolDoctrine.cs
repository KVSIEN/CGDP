using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction's enemies find their own way round (EnemyExploration): with no patrol route
    // they explore the ground near their post, and with one they now and then step off it to
    // somewhere their experience points to. Where they go is drawn by ground they have not
    // seen lately and by places they know are good (GroundDoctrine's values), and pushed off by
    // places where blood was spilt.
    [Serializable]
    public sealed class PatrolDoctrine
    {
        [Tooltip("With no patrol route, explore the ground round its post. Off = it guards its post instead")]
        [SerializeField] private bool _exploreWithoutRoute = true;
        [Tooltip("On a patrol route: the chance, at each waypoint, to detour to somewhere its experience points to before carrying on with the route (0 = it keeps strictly to the route)")]
        [SerializeField, Range(0f, 1f)] private float _routeDrift = 0.25f;
        [Tooltip("Metres from its post (or the nearest waypoint) it ranges. A faction held to its territory also stays inside that")]
        [SerializeField, Min(5f)] private float _range = 20f;
        [Tooltip("How much ground it has not seen lately draws it, against places it knows are good (0 = only favourite places, 1 = only the unexplored)")]
        [SerializeField, Range(0f, 1f)] private float _curiosity = 0.5f;
        [Tooltip("Seconds until ground it has walked counts as unseen again")]
        [SerializeField, Min(10f)] private float _forgetAfter = 120f;

        public PatrolDoctrine() { }

        public PatrolDoctrine(bool exploreWithoutRoute, float routeDrift, float range, float curiosity, float forgetAfter)
        {
            _exploreWithoutRoute = exploreWithoutRoute;
            _routeDrift          = routeDrift;
            _range               = range;
            _curiosity           = curiosity;
            _forgetAfter         = forgetAfter;
        }

        public bool  ExploreWithoutRoute => _exploreWithoutRoute;
        public float RouteDrift          => _routeDrift;
        public float Range               => _range;
        public float Curiosity           => _curiosity;
        public float ForgetAfter         => _forgetAfter;
    }
}
