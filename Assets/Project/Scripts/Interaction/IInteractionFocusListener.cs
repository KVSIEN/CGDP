namespace CGD.Interaction
{
    // Optional companion to IInteractable for feedback while the player is aiming at an
    // object (highlights, hover sounds, NPC turning to look). Implement on any component
    // on the interactable's collider GameObject.
    public interface IInteractionFocusListener
    {
        void OnInteractionFocus(bool focused);
    }
}
