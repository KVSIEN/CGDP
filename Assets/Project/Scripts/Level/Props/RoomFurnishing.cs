using System;
using System.Collections.Generic;

namespace CGD.Level
{
    // What RoomFurnisher planned for one room: the props, and how each placement fared.
    public sealed class RoomFurnishing
    {
        public static readonly RoomFurnishing Empty = new(Array.Empty<PlannedProp>(), Array.Empty<PropPlacementOutcome>());

        public RoomFurnishing(IReadOnlyList<PlannedProp> props, IReadOnlyList<PropPlacementOutcome> outcomes)
        {
            Props    = props;
            Outcomes = outcomes;
        }

        public IReadOnlyList<PlannedProp>          Props    { get; }
        public IReadOnlyList<PropPlacementOutcome> Outcomes { get; }
    }
}
