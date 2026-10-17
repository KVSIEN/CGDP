using CGD.Player;

namespace CGD.Weapons
{
    // The things the player does that take the hands off a reload or a draw, noticed as they
    // start: a dodge, a sprint, or losing control (a stun, a mantle, a committed roll). Ticked
    // every frame, even unarmed, so something already under way at a weapon swap doesn't count
    // as starting during the draw.
    public sealed class PlayerInterrupts
    {
        private bool _wasDodging;
        private bool _wasSprinting;
        private bool _couldAct = true;

        public bool DodgeStarted  { get; private set; }
        public bool SprintStarted { get; private set; }
        public bool ControlLost   { get; private set; }

        // Either part may be missing.
        public void Tick(PlayerDodge dodge, PlayerMovement movement)
        {
            bool dodging = dodge != null && dodge.IsDrivingMovement;
            DodgeStarted = dodging && !_wasDodging;
            _wasDodging  = dodging;

            bool sprinting = movement != null && movement.IsSprinting;
            SprintStarted  = sprinting && !_wasSprinting;
            _wasSprinting  = sprinting;

            bool canAct  = movement == null || movement.CanAct;
            ControlLost  = _couldAct && !canAct;
            _couldAct    = canAct;
        }
    }
}
