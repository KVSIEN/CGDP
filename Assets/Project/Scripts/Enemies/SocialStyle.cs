namespace CGD.Enemies
{
    // How an enemy relates to others of its kind (EnemyData › Social). Callouts only ever pass
    // between kin: the same team, plus any team listed in its Pacts.
    public enum SocialStyle
    {
        // Calls out when it engages and answers calls: how regular enemies behave.
        Standard,
        // A union: shares what it sees with its kin continuously over a long-range link, through
        // walls, and pulls toward the target its kin are already fighting.
        Network,
        // Acts on its own accord: neither calls nor answers.
        Loner,
        // A herd: keeps close to its kin and commits to a fight together; alone and cut off, it
        // falls back to the herd first.
        Herd,
    }
}
