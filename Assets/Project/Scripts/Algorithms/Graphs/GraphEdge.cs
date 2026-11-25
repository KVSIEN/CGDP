namespace CGD.Algorithms
{
    // A link between two numbered things (rooms, towns, nodes) and what it costs: its length, or
    // whatever "worse" means for it.
    public readonly struct GraphEdge
    {
        public GraphEdge(int a, int b, float weight)
        {
            A      = a;
            B      = b;
            Weight = weight;
        }

        public int   A      { get; }
        public int   B      { get; }
        public float Weight { get; }

        public override string ToString() => $"{A}–{B} ({Weight})";
    }
}
