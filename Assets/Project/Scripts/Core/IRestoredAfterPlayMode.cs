namespace CGD.Core
{
    // Marks a ScriptableObject that playing changes on purpose — settings the player edits and
    // that are saved elsewhere (PlayerPrefs) — but whose authored values must stay as they are.
    // In the editor it is put back the way it was when Play mode ends, so a tester's own key
    // bindings never end up in the project. Builds are unaffected.
    public interface IRestoredAfterPlayMode { }
}
