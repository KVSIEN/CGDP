namespace CGD.Map
{
    // A locked node carried into a regeneration: the new map gets a node of this type
    // at the closest matching depth, with the same intensity. Pins describe *what*
    // the node is and roughly *where* in the run it sits, not its id — ids and layout
    // change with every seed.
    public readonly struct MapNodePin
    {
        public MapNodePin(MapNodeType type, float progress, bool onMainPath, float intensity)
        {
            Type       = type;
            Progress   = progress;
            OnMainPath = onMainPath;
            Intensity  = intensity;
        }

        public MapNodeType Type       { get; }
        public float       Progress   { get; }
        public bool        OnMainPath { get; }
        public float       Intensity  { get; }
    }
}
