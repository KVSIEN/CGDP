namespace CGD.Level
{
    public enum RoomShapeKind
    {
        Rectangle,
        Ellipse,
        // 45° shapes, measured in tiles so their slanted edges step exactly one tile at a time and
        // become straight diagonal walls (RoomShape › Angled Walls):
        Octagon,       // the rectangle with each corner cut at 45°, half the shorter half-side back
        Diamond,       // a square turned 45°, as big as the shorter side allows
        PointedEnds,   // the rectangle with its two short ends drawn out to 45° points (a hexagon)
    }
}
