using System;
using System.Collections.Generic;

namespace CGD.Level
{
    // What RoomFurnisher planned for one room: the props, and how each placement fared.
    public sealed class RoomFurnishing
    {
        public static readonly RoomFurnishing Empty = new(Array.Empty<PlannedProp>(), Array.Empty<PropPlacementOutcome>());

        public RoomFurnishing(IReadOnlyList<PlannedProp> props, IReadOnlyList<PropPlacementOutcome> outcomes, int attempt = 0, int tries = 1)
        {
            Props    = props;
            Outcomes = outcomes;
            Attempt  = attempt;
            Tries    = tries;
        }

        public IReadOnlyList<PlannedProp>          Props    { get; }
        public IReadOnlyList<PropPlacementOutcome> Outcomes { get; }
        // Which of the tries was kept (0 = the room's own seed), of how many (RoomTraitFit).
        public int Attempt { get; }
        public int Tries   { get; }
    }
}
