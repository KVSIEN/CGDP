using System;

namespace CGD.Enemies
{
    // What an enemy has found a place good for, from standing there and looking (GroundReading).
    [Flags]
    public enum PlaceTraits
    {
        None    = 0,
        Hidden  = 1,   // inside a concealment zone: a place to lie in wait
        Vantage = 2,   // a long lane with something solid right beside it: a firing position
        Tight   = 4,   // walls close on both sides: a corridor or aisle
    }
}
