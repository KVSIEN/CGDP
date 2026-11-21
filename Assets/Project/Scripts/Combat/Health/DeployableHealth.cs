namespace CGD.Combat
{
    // Health for something a character puts down (a Tech survey beacon): it fights for its
    // deployer's side, so that side can't break it and everyone else can — and sees it as a
    // hostile worth shooting. The team is set each time it is deployed.
    public class DeployableHealth : SimpleHealth
    {
        private Team _team = Team.None;

        public DeployableHealth() : base(60f) { }

        public override Team Team => _team;

        // Put down fresh for `team`: full health again (it may be a pooled instance that was broken).
        public void Deploy(Team team)
        {
            _team = team;
            Revive();
        }
    }
}
