namespace CGD.Level
{
    // One searchable piece as SearchFinds sees it: how likely a find lands in it against the
    // others (its own find weight times its room's), and the finds it adds to the level's.
    public readonly struct SearchSpot
    {
        public SearchSpot(float weight, float findsAdded)
        {
            Weight     = weight < 0f ? 0f : weight;
            FindsAdded = findsAdded < 0f ? 0f : findsAdded;
        }

        public float Weight     { get; }
        public float FindsAdded { get; }
    }
}
