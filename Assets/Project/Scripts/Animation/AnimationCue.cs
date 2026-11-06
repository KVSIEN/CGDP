namespace CGD.Animation
{
    // Moments inside a clip that gameplay or audio reacts to (the foot lands, the magazine
    // leaves the gun). Authored as Animation Events whose string parameter is the cue's name;
    // AnimationEventRelay turns them back into this enum. Add a value here to add a cue.
    public enum AnimationCue
    {
        Footstep,
        Land,
        Draw,
        MagazineOut,
        MagazineIn,
        Chamber,
        Swing,
        Impact,
    }
}
