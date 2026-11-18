namespace CGD.Combat
{
    // None = no allegiance: damage from a None source hits everyone.
    // Enemy is the shared side every regular enemy fights on. Bio, Tech and Void put an
    // enemy on its faction's own side instead, hostile to the player and to the other
    // factions alike (set per EnemyData; used by the AI test scene).
    // Prey is the ecosystem's food: grazers and other creatures that run rather than fight. It is
    // no rival faction, so predators hunt it even while calm (see FactionRelations.KeepsTruce).
    // Values are serialized as numbers: append new teams, never reorder.
    public enum Team
    {
        None,
        Player,
        Enemy,
        Bio,
        Tech,
        Void,
        Prey,
    }
}
