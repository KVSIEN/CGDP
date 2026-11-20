using System;

namespace CGD.Input
{
    // What a BindingTable writes to PlayerPrefs: actions by name, so reordering or adding to an
    // action enum never moves a saved control onto another action.
    [Serializable]
    internal sealed class BindingSaveData
    {
        public Entry[] entries;

        [Serializable]
        public sealed class Entry
        {
            public string action, primaryPath, secondaryPath, mode;
        }
    }
}
