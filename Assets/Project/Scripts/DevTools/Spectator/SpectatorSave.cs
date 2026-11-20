using UnityEngine;

namespace CGD.DevTools
{
    // PlayerPrefs storage for the spectator viewer's preferences, under a key of its own so the
    // player's settings are never touched. Rebound controls are saved by SpectatorInputSettings.
    public static class SpectatorSave
    {
        private const string KeyPreferences = "spectator_prefs";

        public static SpectatorPreferences LoadPreferences(SpectatorInputSettings settings)
        {
            SpectatorPreferences preferences = SpectatorPreferences.DefaultsFrom(settings);
            if (PlayerPrefs.HasKey(KeyPreferences))
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(KeyPreferences), preferences);
            preferences.Clamp();
            return preferences;
        }

        public static void SavePreferences(SpectatorPreferences preferences)
        {
            PlayerPrefs.SetString(KeyPreferences, JsonUtility.ToJson(preferences));
            PlayerPrefs.Save();
        }
    }
}
