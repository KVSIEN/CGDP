namespace CGD.Targeting
{
    // The fixed shape something reaches, for effects that draw its hitbox: a sphere round the
    // caster, or a cone in front of its aim. None when it has no fixed shape (a ray, the
    // nearest few, a spot on the ground it aims at).
    public readonly struct TargetArea
    {
        public enum Kind
        {
            None,
            AroundCaster,
            Cone,
        }

        public static readonly TargetArea None = default;

        public Kind  Shape  { get; }
        // Metres: the sphere's radius, or how far the cone reaches.
        public float Radius { get; }
        // Degrees: the cone's full angle (90 = 45° either side of the aim). 0 for a sphere.
        public float Angle  { get; }

        private TargetArea(Kind shape, float radius, float angle)
        {
            Shape  = shape;
            Radius = radius;
            Angle  = angle;
        }

        public static TargetArea AroundCaster(float radius) => new(Kind.AroundCaster, radius, 0f);
        public static TargetArea Cone(float range, float angle) => new(Kind.Cone, range, angle);
    }
}
