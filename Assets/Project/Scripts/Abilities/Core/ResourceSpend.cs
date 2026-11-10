namespace CGD.Abilities
{
    // The maths of an ability that scales with how much of its resource it spends: it spends
    // everything available between its cost and its maximum, and its power rises linearly
    // from 1× at the cost to MaxPower at the maximum.
    public static class ResourceSpend
    {
        public static float Amount(float available, float cost, float maxSpend)
        {
            if (maxSpend <= cost) return cost;
            return available < cost ? cost : System.Math.Min(available, maxSpend);
        }

        public static float Power(float spent, float cost, float maxSpend, float maxPower)
        {
            if (maxSpend <= cost) return 1f;
            float t = (spent - cost) / (maxSpend - cost);
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return 1f + (maxPower - 1f) * t;
        }
    }
}
