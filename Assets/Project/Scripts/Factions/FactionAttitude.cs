namespace CGD.Factions
{
    // What being hostile to another team means for one faction. Every faction is hostile to
    // every other; the attitude says whether it goes looking for that fight.
    // Values are serialized as numbers: append new attitudes, never reorder.
    public enum FactionAttitude
    {
        // Engages it when they meet, like any hostile.
        Fight,
        // Prefers it over other hostiles: its favourite prey.
        Hunt,
        // Won't start a fight with it; only fights back when touched or hurt by it.
        Avoid,
        // As Avoid, and sensing it in a fight wears its nerve down until it breaks and runs.
        Flee,
    }
}
