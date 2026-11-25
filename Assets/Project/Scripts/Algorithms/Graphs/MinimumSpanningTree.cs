using System.Collections.Generic;

namespace CGD.Algorithms
{
    // A minimum spanning tree: from all the possible links between some things, the cheapest set
    // of links that still connects every one of them, with no loops. Built with Kruskal's
    // algorithm (Joseph Kruskal, 1956).
    //
    // How it works: sort the links from cheapest to dearest and go through them in that order,
    // keeping a link only if it joins two things that aren't connected yet (a DisjointSet answers
    // that instantly). A link between things already connected would only close a loop, so it is
    // skipped. Stop once everything is connected.
    //
    // Why it's useful: the classic way to connect a scatter of rooms, caves or towns with
    // corridors or roads: everything is reachable, nothing is wasted, and the short links win, so
    // the layout looks sensible. A tree has exactly one route between any two places, which
    // plays as a maze of dead ends; add back a few of the skipped links (cheapest first, or at
    // random) for loops and shortcuts. Also: power lines, pipe networks, a quick "is everything
    // connected?" check.
    public static class MinimumSpanningTree
    {
        // The links kept, cheapest first. If the links don't connect everything, the result spans
        // each connected group separately (a "forest").
        public static List<GraphEdge> Kruskal(int nodeCount, IEnumerable<GraphEdge> edges)
        {
            var sorted = new List<GraphEdge>(edges);
            // Ties broken by the ends, so equal weights always come out in the same order.
            sorted.Sort((x, y) =>
            {
                int byWeight = x.Weight.CompareTo(y.Weight);
                if (byWeight != 0) return byWeight;
                int byA = x.A.CompareTo(y.A);
                return byA != 0 ? byA : x.B.CompareTo(y.B);
            });

            var groups = new DisjointSet(nodeCount);
            var tree = new List<GraphEdge>(nodeCount - 1);
            foreach (GraphEdge edge in sorted)
            {
                if (groups.GroupCount == 1) break;   // everything is connected
                if (groups.Union(edge.A, edge.B)) tree.Add(edge);
            }
            return tree;
        }
    }
}
