using System;

namespace CGD.Items
{
    // A saved stack: which stackable item (ItemCatalog id), and how many.
    [Serializable]
    public struct StackRecord
    {
        public string item;
        public int    count;
    }
}
