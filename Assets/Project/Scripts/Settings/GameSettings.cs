using UnityEngine;
using CGD.Audio;

namespace CGD.Settings
{
    // The player's audio, video and accessibility preferences for the whole application.
    // Static because they belong to the player, not to a scene: systems read Current when
    // they need a value (a sound playing, a flash, the camera's FOV) instead of caching it,
    // so a change in the settings menu takes effect everywhere at once.
    public static class GameSettings
    {
        private const string Key = "game_settings";

        private static SettingsData _current;

        public static SettingsData Current => _current ??= Load();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _current = null;

        // Before the first scene so the window, quality level and volume are right from frame one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnStartup() => ApplyToEngine(Current);

        // Replaces the settings and pushes them to the engine, so the menu can preview a change
        // live; call Save once the player is done editing.
        public static void Apply(SettingsData data)
        {
            _current = data.Clone();
            ApplyToEngine(_current);
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static float Volume(AudioCategory category) => category switch
        {
            AudioCategory.Music     => Current.MusicVolume,
            AudioCategory.Interface => Current.InterfaceVolume,
            _                       => Current.EffectsVolume,
        };

        private static SettingsData Load()
        {
            if (!PlayerPrefs.HasKey(Key)) return new SettingsData();
            return JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(Key)) ?? new SettingsData();
        }

        private static void ApplyToEngine(SettingsData data)
        {
            AudioListener.volume = Mathf.Clamp01(data.MasterVolume);

            if (data.QualityLevel >= 0 && data.QualityLevel < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(data.QualityLevel, applyExpensiveChanges: true);

            QualitySettings.vSyncCount  = data.VSync ? 1 : 0;
            Application.targetFrameRate = data.VSync || data.FrameRateLimit <= 0 ? -1 : data.FrameRateLimit;

            // The editor's Game view ignores window changes; builds apply them.
            if (Application.isEditor) return;

            int width  = data.ResolutionWidth  > 0 ? data.ResolutionWidth  : Screen.width;
            int height = data.ResolutionHeight > 0 ? data.ResolutionHeight : Screen.height;
            // Only when something changed — re-setting the same mode still flickers the window.
            if (width == Screen.width && height == Screen.height && data.WindowMode == Screen.fullScreenMode) return;
            Screen.SetResolution(width, height, data.WindowMode);
        }
    }
}
