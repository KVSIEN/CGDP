using System;
using UnityEngine;

namespace CGD.DevTools
{
    // PlayerPrefs storage for the spectator: its rebound controls and the viewer's
    // preferences, under keys of their own so the player's settings are never touched.
    public static class SpectatorSave
    {
        private const string KeyBindings    = "spectator_bindings";
        private const string KeyPreferences = "spectator_prefs";

        public static void SaveBindings(SpectatorInputSettings settings)
        {
            var data = new SaveData { entries = new Entry[settings.Bindings.Count] };
            for (int i = 0; i < settings.Bindings.Count; i++)
            {
                SpectatorBinding binding = settings.Bindings[i];
                data.entries[i] = new Entry { action = binding.Action.ToString(), primary = binding.PrimaryPath, secondary = binding.SecondaryPath };
            }
            PlayerPrefs.SetString(KeyBindings, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        // Applies saved controls over the asset's (in memory only). False if none were saved.
        public static bool LoadBindings(SpectatorInputSettings settings)
        {
            if (!PlayerPrefs.HasKey(KeyBindings)) return false;
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KeyBindings));
            if (data?.entries == null) return false;

            foreach (Entry entry in data.entries)
            {
                if (!Enum.TryParse(entry.action, out SpectatorAction action)) continue;
                int index = settings.IndexOf(action);
                if (index < 0) continue;

                SpectatorBinding binding = settings.Bindings[index];
                binding.PrimaryPath   = entry.primary;
                binding.SecondaryPath = entry.secondary;
                settings.Bindings[index] = binding;
            }
            return true;
        }

        public static void DeleteBindings() => PlayerPrefs.DeleteKey(KeyBindings);

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

        [Serializable] private class SaveData { public Entry[] entries; }

        [Serializable]
        private class Entry
        {
            public string action, primary, secondary;
        }
    }
}
