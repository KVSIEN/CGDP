namespace CGD.Enemies
{
    // Where a faction likes to go when it explores on its own (PatrolDoctrine › Style), on top of
    // the ground it has not seen lately (see PatrolScoring.StyleBonus).
    public enum ExploreStyle
    {
        Roam,       // anywhere in range, nearer first
        Expand,     // a little at a time: short hops round what it already knows, its range growing as it learns (TECH)
        Outskirts,  // the edges of its range, along walls (VOID)
        Inside,     // the open middle of its ground and round the concealment it knows (BIO)
    }
}
