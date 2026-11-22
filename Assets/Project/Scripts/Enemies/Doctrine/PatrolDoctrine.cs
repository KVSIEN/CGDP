using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction's enemies find their own way round (EnemyExploration): with no patrol route
    // they explore the ground near their post, and with one they now and then step off it to
    // somewhere their experience points to. Where they go is drawn by ground they have not
    // seen lately and by places they know are good (GroundDoctrine's values), and pushed off by
    // places where blood was spilt; each faction also has its own taste in where to look (Style).
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
        [Tooltip("How it walks from stop to stop and back to its post: Direct (the shortest way), Through Bushes (from bush to bush on the way, where its data allows the detour) or Along Walls (keeping to walls and obstacle edges)")]
        [SerializeField] private RouteStyle _walk = RouteStyle.ThroughBushes;
        [Tooltip("Each time it chooses where to go: the chance it heads for something it likes instead (a PointOfInterest within reach), to spend a while with it")]
        [SerializeField, Range(0f, 1f)] private float _interestChance = 0.35f;
        [Tooltip("Where it likes to explore, on top of unseen ground: Roam (anywhere, nearer first), Expand (short hops round what it knows, its range growing as it learns), Outskirts (the edges of its range, along walls) or Inside (the open middle and round concealment it knows)")]
        [SerializeField] private ExploreStyle _style = ExploreStyle.Roam;

        public PatrolDoctrine() { }

        public PatrolDoctrine(bool exploreWithoutRoute, float routeDrift, float range, float curiosity, float forgetAfter,
                              RouteStyle walk = RouteStyle.ThroughBushes, float interestChance = 0.35f,
                              ExploreStyle style = ExploreStyle.Roam)
        {
            _exploreWithoutRoute = exploreWithoutRoute;
            _routeDrift          = routeDrift;
            _range               = range;
            _curiosity           = curiosity;
            _forgetAfter         = forgetAfter;
            _walk                = walk;
            _interestChance      = interestChance;
            _style               = style;
        }

        public bool  ExploreWithoutRoute => _exploreWithoutRoute;
        public float RouteDrift          => _routeDrift;
        public float Range               => _range;
        public float Curiosity           => _curiosity;
        public float ForgetAfter         => _forgetAfter;
        public RouteStyle Walk           => _walk;
        public float InterestChance      => _interestChance;
        public ExploreStyle Style        => _style;
    }
}
