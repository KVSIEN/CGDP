namespace CGD.Enemies
{
    // Who does what in a squad's fight — pure rules, so they can be tested:
    //   ranged members  the nearest `suppressors` pin the target down; the rest flank
    //   melee members   the nearest `assaulters` go straight in; the rest flank
    // "Nearest" is by distance to where each believes the target is, so whoever is closest
    // engages and the others spread out.
    public static class SquadRoles
    {
        public static void Assign(bool[] ranged, float[] distance, int suppressors, int assaulters, SquadRole[] roles)
        {
            int count = ranged.Length;
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            System.Array.Sort(order, (a, b) => distance[a].CompareTo(distance[b]));

            int suppressing = 0, assaulting = 0;
            foreach (int member in order)
            {
                if (ranged[member]) roles[member] = suppressing++ < suppressors ? SquadRole.Suppress : SquadRole.Flank;
                else                roles[member] = assaulting++  < assaulters  ? SquadRole.Assault  : SquadRole.Flank;
            }
        }
    }
}
