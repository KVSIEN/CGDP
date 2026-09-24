using UnityEngine;
using CGD.Combat;

namespace CGD.Targeting
{
    // Where a targeting query starts and who is asking. Origin/Direction are the aim
    // (usually the camera, or an enemy's eyes); Caster is the character doing the asking,
    // which decides Self and the ally/enemy split.
    public readonly struct TargetingRequest
    {
        public readonly Vector3       Origin;
        public readonly Vector3       Direction;
        public readonly HealthManager Caster;

        public TargetingRequest(Vector3 origin, Vector3 direction, HealthManager caster)
        {
            Origin    = origin;
            Direction = direction;
            Caster    = caster;
        }

        public Team Team => Caster != null ? Caster.Team : Team.None;

        // Where the caster physically stands (the aim origin may be a camera above it).
        public Vector3 CasterPosition => Caster != null ? Caster.transform.position : Origin;
    }
}
