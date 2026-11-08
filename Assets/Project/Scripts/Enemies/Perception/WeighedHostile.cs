using CGD.Combat;

namespace CGD.Enemies
{
    // One hostile its senses weighed on the last update: how much its target priorities
    // favour it (TargetWeights, 1 = neutral) and whether it was seen or only heard. Read by
    // the spectator's overlays and inspector.
    public readonly struct WeighedHostile
    {
        public readonly HealthManager Hostile;
        public readonly float         Weight;
        public readonly bool          Seen;
        // Seen or heard at all (an unsensed hostile nearby scores nothing).
        public readonly bool          Sensed;

        public WeighedHostile(HealthManager hostile, float weight, bool seen, bool sensed)
        {
            Hostile = hostile;
            Weight  = weight;
            Seen    = seen;
            Sensed  = sensed;
        }
    }
}
