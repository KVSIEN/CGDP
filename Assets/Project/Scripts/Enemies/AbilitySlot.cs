namespace CGD.Enemies
{
    // Which ability an ability node uses: the one it names itself, or one of the enemy's own
    // kit (EnemyData › Kit). Naming a kit slot keeps a behavior faction-neutral — the same
    // "escape when hurt" runs a Phase Shift on one enemy type and an Ambush on another.
    public enum AbilitySlot
    {
        // The Ability set on the node.
        Own,
        // Its signature attack (a Rift, a Snare, a Discharge…).
        Signature,
        // How it gets away (a Phase Shift, an Ambush, an Echo…).
        Escape,
        // What it calls up (a Summon ability).
        Summon,
        // A buff on itself (a Fortify, a Surge, an Overdrive…).
        Boost,
        // What it does for an ally (a Repair, a heal on allies…).
        Support,
    }
}
