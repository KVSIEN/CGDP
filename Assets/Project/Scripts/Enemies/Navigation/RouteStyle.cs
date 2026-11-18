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
        // Keeping to the walls: in legs of a few metres, each pulled to the nearest wall or
        // obstacle edge if one is close to the way (a sentry sweeping the perimeter). Where there
        // are no walls near the way it is Direct.
        AlongWalls,
    }
}
