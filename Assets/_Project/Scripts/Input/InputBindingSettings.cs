using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InputBindingSettings", menuName = "CGD/Input Binding Settings")]
public class InputBindingSettings : ScriptableObject
{
    public List<ActionBinding> Bindings = new();

    public bool TryGet(GameAction action, out ActionBinding binding)
    {
        for (int i = 0; i < Bindings.Count; i++)
        {
            if (Bindings[i].Action != action) continue;
            binding = Bindings[i];
            return true;
        }
        binding = default;
        return false;
    }

    [ContextMenu("Clear Saved Bindings (PlayerPrefs)")]
    public void ClearSavedBindings() => SettingsSave.DeleteBindings();

    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        Bindings = new List<ActionBinding>
        {
            new() { Action = GameAction.Jump,              PrimaryPath = "<Keyboard>/space",                                   Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Sprint,            PrimaryPath = "<Keyboard>/leftShift",                               Mode = InputActionMode.Held    },
            new() { Action = GameAction.Crouch,            PrimaryPath = "<Keyboard>/leftCtrl", SecondaryPath = "<Keyboard>/c", Mode = InputActionMode.Toggle  },
            new() { Action = GameAction.Dodge,             PrimaryPath = "<Keyboard>/q",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Attack,            PrimaryPath = "<Mouse>/leftButton",                                 Mode = InputActionMode.Held    },
            new() { Action = GameAction.Melee,             PrimaryPath = "<Keyboard>/g",                                       Mode = InputActionMode.Held    },
            new() { Action = GameAction.Grenade,           PrimaryPath = "<Keyboard>/h",                                       Mode = InputActionMode.Held    },
            new() { Action = GameAction.AimDownSights,     PrimaryPath = "<Mouse>/rightButton",                                Mode = InputActionMode.Held    },
            new() { Action = GameAction.Reload,            PrimaryPath = "<Keyboard>/r",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Interact,          PrimaryPath = "<Keyboard>/e",         SecondaryPath = "<Keyboard>/f", Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability1,          Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability2,          Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability3,          Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability4,          Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Pause,             PrimaryPath = "<Keyboard>/escape",                                  Mode = InputActionMode.Pressed },
            new() { Action = GameAction.TogglePerspective, PrimaryPath = "<Keyboard>/v",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Inventory,         PrimaryPath = "<Keyboard>/i",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Map,               PrimaryPath = "<Keyboard>/m",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.ShoulderSwap,      PrimaryPath = "<Keyboard>/x",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Weapon1,           PrimaryPath = "<Keyboard>/1",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Weapon2,           PrimaryPath = "<Keyboard>/2",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Weapon3,           PrimaryPath = "<Keyboard>/3",                                       Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Weapon4,           PrimaryPath = "<Keyboard>/4",                                       Mode = InputActionMode.Pressed },
        };
    }
}

// PrimaryPath/SecondaryPath are Input System control paths (e.g. "<Keyboard>/g",
// "<Mouse>/leftButton", "<Gamepad>/buttonSouth") — the same format
// InputAction.AddBinding expects and InputActionRebindingExtensions.PerformInteractiveRebinding
// resolves to directly, so no hand-rolled enum-to-path translation is needed anywhere.
[Serializable]
public struct ActionBinding
{
    public GameAction Action;
    public string PrimaryPath;
    public string SecondaryPath;
    public InputActionMode Mode;
}
