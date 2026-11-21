namespace CGD.Combat
{
    // Health for breakable props — crates, barrels, generators. No team, so anyone's
    // attacks break it. Pair with DespawnOnDeath to remove the wreck and LootDropper to
    // spill its contents.
    public class Destructible : SimpleHealth
    {
        public Destructible() : base(50f) { }

        public override Team Team => Team.None;
    }
}
