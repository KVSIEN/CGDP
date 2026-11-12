namespace CGD.Enemies
{
    // How an encounter (kin of one faction fighting the same target) carries itself as a group.
    public enum EncounterPosture
    {
        // Fighting as usual, roles as the faction hands them out.
        Steady,
        // Its lead just fell (or its overseer pulled it back): the rest hold and re-form on
        // the faction's regroup roles while whoever stepped up takes charge.
        Regroup,
    }
}
