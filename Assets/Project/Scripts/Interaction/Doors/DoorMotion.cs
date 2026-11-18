namespace CGD.Interaction
{
    // How a DoorPanel moves from closed to open, in the panel's parent's space: a swing
    // turns about the panel's own origin (the hinge); slides move it along that axis.
    public enum DoorMotion
    {
        Swing,
        SlideUp,
        SlideDown,
        SlideLeft,
        SlideRight,
    }
}
