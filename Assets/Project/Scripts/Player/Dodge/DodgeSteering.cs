namespace CGD.Player
{
    // How much a dodge stage follows the movement input once it has started.
    public enum DodgeSteering
    {
        Locked,     // keeps the direction it started in
        TurnRate,   // turns toward the input at the stage's turn rate
        Free,       // snaps to the input every step
    }
}
