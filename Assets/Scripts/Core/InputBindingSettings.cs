using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        Bindings = new List<ActionBinding>
        {
            new() { Action = GameAction.Jump,              PrimaryKey = Key.Space,                              Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Sprint,            PrimaryKey = Key.LeftShift,                          Mode = InputActionMode.Held    },
            new() { Action = GameAction.Crouch,            PrimaryKey = Key.LeftCtrl,  SecondaryKey = Key.C,    Mode = InputActionMode.Toggle  },
            new() { Action = GameAction.Dodge,             PrimaryKey = Key.Q,                                  Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Attack,            PrimaryMouse = InputMouseButton.Left,                Mode = InputActionMode.Held    },
            new() { Action = GameAction.AimDownSights,     PrimaryMouse = InputMouseButton.Right,               Mode = InputActionMode.Held    },
            new() { Action = GameAction.Reload,            PrimaryKey = Key.R,                                  Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Interact,          PrimaryKey = Key.E,         SecondaryKey = Key.F,    Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability1,          PrimaryKey = Key.Digit1,                             Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability2,          PrimaryKey = Key.Digit2,                             Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability3,          PrimaryKey = Key.Digit3,                             Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Ability4,          PrimaryKey = Key.Digit4,                             Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Pause,             PrimaryKey = Key.Escape,                             Mode = InputActionMode.Pressed },
            new() { Action = GameAction.TogglePerspective, PrimaryKey = Key.V,                                  Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Inventory,         PrimaryKey = Key.Tab,                                Mode = InputActionMode.Pressed },
            new() { Action = GameAction.Map,               PrimaryKey = Key.M,                                  Mode = InputActionMode.Pressed },
        };
    }
}

[Serializable]
public struct ActionBinding
{
    public GameAction Action;
    public Key PrimaryKey;
    public InputMouseButton PrimaryMouse;
    public Key SecondaryKey;
    public InputMouseButton SecondaryMouse;
    public InputActionMode Mode;
}
