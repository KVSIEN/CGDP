using UnityEngine;
using CGD.Combat;

namespace CGD.Enemies
{
    // What an enemy tells its kin (EnemyCallouts), by its SocialStyle: a loner says nothing,
    // a regular enemy shouts when it engages, and a union's link reaches every member through
    // walls and shares each sighting as it happens, not just the first one.
    internal sealed class CalloutSender
    {
        private const float LinkRange = 80f;   // metres a union's link (and a radio call) reaches
        private const float LinkEvery = 1f;    // seconds between a union's shared sightings

        private readonly EnemyAI           _owner;
        private readonly Transform         _self;
        private readonly EnemyData         _data;
        private readonly PerceptionProfile _profile;
        private readonly Team              _team;

        private float _nextLink;

        public CalloutSender(EnemyAI owner, EnemyData data, PerceptionProfile profile, Team team)
        {
            _owner   = owner;
            _self    = owner.transform;
            _data    = data;
            _profile = profile;
            _team    = team;
        }

        private bool IsLoner  => _data.Social == SocialStyle.Loner;
        private bool IsLinked => _data.Social == SocialStyle.Network;

        // It has just engaged a hostile standing here.
        public void Spotted(Vector3 target)
        {
            if (IsLoner) return;

            float radius = (IsLinked ? LinkRange : _profile.CalloutRadius) * _owner.Doctrine.Escalation.CalloutRadiusScale;
            Raise(target, radius, throughWalls: IsLinked);
        }

        // It sees its target again (clock: the perception's own); a union passes it on.
        public void Sighted(Vector3 target, float clock)
        {
            if (!IsLinked || clock < _nextLink) return;
            _nextLink = clock + LinkEvery;
            Spotted(target);
        }

        // Losing its nerve: tells its kin where the trouble is, over the radio, walls or not.
        public void CallForHelp(Vector3 trouble)
        {
            if (!IsLoner) Raise(trouble, LinkRange, throughWalls: true);
        }

        // Found one of its own dead: reports where, so its kin come to look.
        public void ReportBody(Vector3 body)
        {
            if (!IsLoner) Raise(body, LinkRange, throughWalls: true);
        }

        private void Raise(Vector3 reported, float radius, bool throughWalls) =>
            EnemyCallouts.Raise(new EnemyCallout(_owner, _self.position, reported, _team, radius, throughWalls));
    }
}
