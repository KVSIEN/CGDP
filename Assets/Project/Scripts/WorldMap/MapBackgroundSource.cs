namespace CGD.WorldMap
{
    // Where WorldMapArea gets the picture under the fog and markers.
    public enum MapBackgroundSource
    {
        // A top-down orthographic render of the level, taken once when the scene starts.
        Capture,
        // A hand-made image covering the area.
        Texture,
        // The map graph (rooms and connections) drawn as a schematic — for procedural runs
        // that have a graph but no painted map.
        MapGraph,
        // Just the background colour.
        None
    }
}
