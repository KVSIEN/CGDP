namespace CGD.Enemies
{
    // A position an enemy can store on its blackboard (Remember Position).
    public enum RememberedPosition
    {
        // Where it stands now.
        Self,
        // Its post (where it spawned).
        Post,
        // Where it believes its target is (exact only while it senses it).
        Target,
        // Where the last thing it noticed came from.
        Disturbance,
    }
}
