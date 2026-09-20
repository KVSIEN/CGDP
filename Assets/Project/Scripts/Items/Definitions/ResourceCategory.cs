namespace CGD.Items
{
    // Broad groupings drawn from the GDD's per-reality resource tables. Kept
    // reality-agnostic so one category can span realities (metal is Raw whether it
    // was mined in TECH or scavenged in VOID). Append as the resource list grows.
    public enum ResourceCategory
    {
        Raw       = 0,
        Refined   = 1,
        Energy    = 2,
        Organic   = 3,
        Anomalous = 4,
        Relic     = 5,
    }
}
