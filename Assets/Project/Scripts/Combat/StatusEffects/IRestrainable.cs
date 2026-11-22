namespace CGD.Combat
{
    // Something that can be held in place until it struggles free (the player), as opposed to
    // merely stunned. Lives on the same object as its StatusEffectController.
    public interface IRestrainable
    {
        // Holds it for at most `maxSeconds` (0 = until it breaks free), breaking free after
        // `tapsRequired` taps (0 = its own default). `label` is what its prompt says.
        void Restrain(string label, int tapsRequired, float maxSeconds);
    }
}
