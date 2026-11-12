namespace CGD.Enemies
{
    // Something an enemy says out loud. Most are said on their own when the enemy does the
    // thing (EnemyVoice); Pushing and HoldingBack come from the director or a Bark node.
    public enum BarkKind
    {
        None,
        Spotted,        // just saw a hostile
        Investigating,  // heard or glimpsed something, going to look
        Hunting,        // knows someone is about, searching hard
        LostTarget,     // the target slipped out of sight mid-fight
        NothingFound,   // gave up a search
        Victory,        // its target is down
        Flanking,       // circling round
        Suppressing,    // laying down fire to pin the target
        Grenade,        // throwing one
        FallingBack,    // morale broke
        RaisingAlarm,   // running for an alarm panel
        Pushing,        // the director's peak: "go, go!"
        HoldingBack,    // the director's calm: "hold position"
        Ambient,        // idle muttering while nothing is going on
        Warning,        // a grenade is about to go off
    }
}
