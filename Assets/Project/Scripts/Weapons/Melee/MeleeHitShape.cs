namespace CGD.Weapons
{
    public enum MeleeHitShape
    {
        // SphereCast forward — stabs, pokes, jabs. Narrow but long, resolves
        // hitbox regions via point hit.
        Thrust,
        // Arc of SphereCasts fanned horizontally — slashes, wide swings.
        // Each ray resolves hitbox regions independently.
        Sweep,
        // OverlapSphere at the impact point — overhead smashes, ground pounds.
        // Area damage, no region resolution.
        Slam,
    }
}
