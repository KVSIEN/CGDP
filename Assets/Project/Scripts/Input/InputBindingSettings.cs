using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Input
{
    [CreateAssetMenu(fileName = "InputBindingSettings", menuName = "CGD/Input/Input Binding Settings")]
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
            // Keyboard plus left/right mouse only: extra mouse buttons aren't on every mouse.
            Bindings = new List<ActionBinding>
            {
                new() { Action = GameAction.Jump,              PrimaryPath = "<Keyboard>/space",     Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Sprint,            PrimaryPath = "<Keyboard>/leftShift", Mode = InputActionMode.Held    },
                new() { Action = GameAction.Crouch,            PrimaryPath = "<Keyboard>/leftCtrl",  Mode = InputActionMode.Toggle  },
                new() { Action = GameAction.Dodge,             PrimaryPath = "<Keyboard>/c",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Attack,            PrimaryPath = "<Mouse>/leftButton",   Mode = InputActionMode.Held    },
                new() { Action = GameAction.Melee,             PrimaryPath = "<Keyboard>/v",         Mode = InputActionMode.Held    },
                new() { Action = GameAction.AimDownSights,     PrimaryPath = "<Mouse>/rightButton",  Mode = InputActionMode.Held    },
                new() { Action = GameAction.Reload,            PrimaryPath = "<Keyboard>/r",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Interact,          PrimaryPath = "<Keyboard>/e",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Ability1,          PrimaryPath = "<Keyboard>/q",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Ability2,          PrimaryPath = "<Keyboard>/f",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Ability3,          PrimaryPath = "<Keyboard>/g",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Ability4,          PrimaryPath = "<Keyboard>/x",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Pause,             PrimaryPath = "<Keyboard>/escape",    Mode = InputActionMode.Pressed },
                new() { Action = GameAction.TogglePerspective, PrimaryPath = "<Keyboard>/f5",        Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Inventory,         PrimaryPath = "<Keyboard>/i",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Map,               PrimaryPath = "<Keyboard>/m",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.ShoulderSwap,      PrimaryPath = "<Keyboard>/h",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Weapon1,           PrimaryPath = "<Keyboard>/1",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Weapon2,           PrimaryPath = "<Keyboard>/2",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Weapon3,           PrimaryPath = "<Keyboard>/3",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Weapon4,           PrimaryPath = "<Keyboard>/4",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.LockOn,            PrimaryPath = "<Keyboard>/t",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Console,           PrimaryPath = "<Keyboard>/backquote", Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Character,         PrimaryPath = "<Keyboard>/tab",       Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Item1,             PrimaryPath = "<Keyboard>/5",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Item2,             PrimaryPath = "<Keyboard>/6",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Item3,             PrimaryPath = "<Keyboard>/7",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.Item4,             PrimaryPath = "<Keyboard>/8",         Mode = InputActionMode.Pressed },
                new() { Action = GameAction.WeaponMode,        PrimaryPath = "<Keyboard>/b",         Mode = InputActionMode.Pressed },
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
}
