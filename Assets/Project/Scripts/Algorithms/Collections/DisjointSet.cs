namespace CGD.Algorithms
{
    // A disjoint set (also called union-find): keeps track of which things are connected to which,
    // as connections are added, and answers "are these two in the same group?" almost instantly.
    //
    // How it works: things are numbered 0 to count - 1. Each group is a little tree, and the
    // thing at its root names the group. Find(a) walks from a up to its root. Union(a, b) hangs
    // one root under the other, the smaller tree under the taller (union by rank) so trees stay
    // flat. And every Find points the things it walked past straight at the root (path
    // compression), so the next walk is a single step. Together these make both operations
    // effectively constant time.
    //
    // Why it's useful: connectivity questions in generation and gameplay. Are all rooms of a map
    // joined up? Which islands of floor are cut off? Which pipes form one network? It is also the
    // heart of Kruskal's minimum spanning tree (MinimumSpanningTree), and of maze generation
    // (knock down a wall only between cells not yet connected).
    public sealed class DisjointSet
    {
        private readonly int[] _parent;
        private readonly int[] _rank;   // an upper bound on each root's tree height

        // `count` things, each in a group of its own.
        public DisjointSet(int count)
        {
            _parent = new int[count];
            _rank   = new int[count];
            for (int i = 0; i < count; i++) _parent[i] = i;
            GroupCount = count;
        }

        public int GroupCount { get; private set; }

        // The thing that names the group `item` is in.
        public int Find(int item)
        {
            int root = item;
            while (_parent[root] != root) root = _parent[root];

            // Path compression: point everything walked past straight at the root.
            while (_parent[item] != root)
            {
                int next = _parent[item];
                _parent[item] = root;
                item = next;
            }
            return root;
        }

        public bool AreConnected(int a, int b) => Find(a) == Find(b);

        // Joins the two groups; false when a and b were already in the same one.
        public bool Union(int a, int b)
        {
            int rootA = Find(a), rootB = Find(b);
            if (rootA == rootB) return false;

            if (_rank[rootA] < _rank[rootB]) (rootA, rootB) = (rootB, rootA);
            _parent[rootB] = rootA;
            if (_rank[rootA] == _rank[rootB]) _rank[rootA]++;
            GroupCount--;
            return true;
        }
    }
}
