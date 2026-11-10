using UnityEngine;

namespace CGD.Combat
{
    // A patch of ground that hurts whoever stands in it (a Void Rift, say), as characters see
    // it: where it is, how far it reaches, how hard it hits and whose it is. Whatever owns the
    // effect places it and lists it in Hazards for as long as it is open.
    public sealed class HazardZone
    {
        // Flat discs on the ground: someone well above or below isn't in it.
        private const float HeightReach = 2.5f;

        public Vector3      Center          { get; private set; }
        public float        Radius          { get; private set; }
        public float        DamagePerSecond { get; private set; }
        public DamageSource Source          { get; private set; }

        public void Place(Vector3 center, float radius, float damagePerSecond, DamageSource source)
        {
            Center          = center;
            Radius          = radius;
            DamagePerSecond = damagePerSecond;
            Source          = source;
        }

        public bool Covers(Vector3 position)
        {
            Vector3 offset = position - Center;
            if (Mathf.Abs(offset.y) > HeightReach) return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= Radius * Radius;
        }

        public bool Hurts(HealthManager victim) => victim != null && victim.CanBeDamagedBy(Source);

        // Whether the straight line from → to passes within `margin` of its edge. `along` is how
        // far along the line (0–1) it comes closest, to tell which of several it meets first.
        public bool Crosses(Vector3 from, Vector3 to, float margin, out float along)
        {
            along = 0f;
            if (Mathf.Abs(from.y - Center.y) > HeightReach && Mathf.Abs(to.y - Center.y) > HeightReach) return false;

            var start  = new Vector2(from.x, from.z);
            var line   = new Vector2(to.x, to.z) - start;
            var center = new Vector2(Center.x, Center.z);
            float lengthSq = line.sqrMagnitude;
            along = lengthSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(center - start, line) / lengthSq) : 0f;

            float reach = Radius + margin;
            return (start + line * along - center).sqrMagnitude <= reach * reach;
        }
    }
}
