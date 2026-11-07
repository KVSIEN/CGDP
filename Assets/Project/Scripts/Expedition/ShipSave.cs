using System;
using CGD.Items;

namespace CGD.Expedition
{
    // What persists between play sessions: the ship's hold and the kit packed for the next run.
    // A run in progress is never saved — quitting mid-run counts as not making it out, as
    // leaving the level does.
    [Serializable]
    public class ShipSave
    {
        // Bump when the layout changes, and teach ShipPersistence to read the old one.
        public const int CurrentVersion = 1;

        public int             version = CurrentVersion;
        public InventoryRecord hold    = new();
        public InventoryRecord kit     = new();
    }
}
