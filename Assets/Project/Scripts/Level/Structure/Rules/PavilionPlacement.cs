namespace CGD.Level
{
    // Where in a room its pavilions stand, which sets how the room plays round them.
    public enum PavilionPlacement
    {
        Anywhere,   // scattered blocks that break the floor into pockets
        Centre,     // a hub in the middle that the room circles
        Perimeter,  // along the walls, alcoves off an open middle
    }
}
