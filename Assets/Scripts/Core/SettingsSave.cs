using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class SettingsSave
{
    private const string KeyMouse    = "sens_mouse";
    private const string KeyGamepad  = "sens_gamepad";
    private const string KeyBindings = "keybindings";

    public static void SaveSensitivity(float mouse, float gamepad)
    {
        PlayerPrefs.SetFloat(KeyMouse, mouse);
        PlayerPrefs.SetFloat(KeyGamepad, gamepad);
        PlayerPrefs.Save();
    }

    public static void LoadSensitivity(out float mouse, out float gamepad)
    {
        mouse   = PlayerPrefs.GetFloat(KeyMouse,   1.5f);
        gamepad = PlayerPrefs.GetFloat(KeyGamepad, 120f);
    }

    public static void SaveBindings(InputBindingSettings settings)
    {
        var data = new SaveData { entries = new BindingEntry[settings.Bindings.Count] };
        for (int i = 0; i < settings.Bindings.Count; i++)
        {
            var b = settings.Bindings[i];
            data.entries[i] = new BindingEntry
            {
                action         = (int)b.Action,
                primaryKey     = (int)b.PrimaryKey,
                primaryMouse   = (int)b.PrimaryMouse,
                secondaryKey   = (int)b.SecondaryKey,
                secondaryMouse = (int)b.SecondaryMouse,
            };
        }
        PlayerPrefs.SetString(KeyBindings, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool LoadBindings(InputBindingSettings settings)
    {
        if (!PlayerPrefs.HasKey(KeyBindings)) return false;
        var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KeyBindings));
        if (data?.entries == null) return false;

        foreach (var entry in data.entries)
        {
            var action = (GameAction)entry.action;
            for (int j = 0; j < settings.Bindings.Count; j++)
            {
                if (settings.Bindings[j].Action != action) continue;
                var b = settings.Bindings[j];
                b.PrimaryKey     = (Key)entry.primaryKey;
                b.PrimaryMouse   = (InputMouseButton)entry.primaryMouse;
                b.SecondaryKey   = (Key)entry.secondaryKey;
                b.SecondaryMouse = (InputMouseButton)entry.secondaryMouse;
                settings.Bindings[j] = b;
                break;
            }
        }
        return true;
    }

    public static void DeleteBindings() => PlayerPrefs.DeleteKey(KeyBindings);

    [Serializable] private class SaveData { public BindingEntry[] entries; }

    [Serializable]
    private class BindingEntry
    {
        public int action, primaryKey, primaryMouse, secondaryKey, secondaryMouse;
    }
}
