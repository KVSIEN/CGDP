using UnityEngine;

// Base class for all abilities. Create a new ability by inheriting from this
// and implementing Execute. Add the CreateAssetMenu attribute so it appears
// in the Project right-click menu.
public abstract class Ability : ScriptableObject
{
    [Header("Info")]
    public string DisplayName = "Ability";
    public Color  SlotColor   = new Color(0.3f, 0.55f, 1f, 1f);

    [Header("Cooldown")]
    public float Cooldown = 5f;

    // Fires the ability. Return true on success — this starts the cooldown.
    // Return false to cancel without triggering the cooldown (e.g. not enough health).
    public abstract bool Execute(AbilityContext ctx);
}
