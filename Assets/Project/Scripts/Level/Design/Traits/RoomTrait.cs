namespace CGD.Level
{
    // What a fight room offers the side that holds it. Each faction leans on one: BIO hides
    // (concealment), VOID breaks sight with walls (corners, narrow ways), TECH holds long lines
    // of fire from hard cover. RoomTraitMeasure scores a room on each.
    public enum RoomTrait
    {
        Concealment,   // floor you can hide in without breaking line of sight: foliage, patches
        Walls,         // narrow floor hemmed in by walls, pillars and furniture: corridors, aisles, corners
        LinesOfFire,   // long open lanes, with hard cover beside them to shoot from
    }
}
