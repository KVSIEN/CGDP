using System;
using CGD.Core;

namespace CGD.Level
{
    // A furnished room whose loose props can be moved one at a time, for RoomPlanRefiner to
    // search over. It knows what a legal move is; the refiner knows what a good one is.
    internal interface IRefinablePlan
    {
        // How many props may be moved.
        int MovableCount { get; }

        // Moves the `index`th movable prop to another spot where it is still legal, picked with
        // `random`, and returns `undo` to put it back as it was. False, with nothing changed,
        // when it has nowhere else to go.
        bool TryMove(int index, RandomStream random, out Action undo);
    }
}
