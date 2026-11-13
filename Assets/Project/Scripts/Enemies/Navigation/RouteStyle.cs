namespace CGD.Enemies
{
    // How an enemy walks to a destination (EnemyRouting.SetDestination).
    public enum RouteStyle
    {
        // The NavMesh's shortest way (round hazards): rushes, dodges, getting clear.
        Direct,
        // Sneaking: from bush to bush on the way, when its data allows a detour for that
        // (EnemyData › Bush Detour). Without bushes or a detour allowance it is Direct.
        ThroughBushes,
    }
}
