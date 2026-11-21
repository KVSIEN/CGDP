using CGD.Combat;

namespace CGD.Level
{
    // What a Siege room asks the player to defend. On the player's team, so enemies see it as
    // a hostile target like any other and attack it, while the player's own shots pass it by.
    // Its collider must be on a layer enemies look for targets on (the player's).
    public class SiegeObjective : SimpleHealth
    {
        public SiegeObjective() : base(400f) { }

        public override Team Team => Team.Player;
    }
}
