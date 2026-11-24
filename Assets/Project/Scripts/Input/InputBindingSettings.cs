using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Input
{
    [CreateAssetMenu(fileName = "InputBindingSettings", menuName = "CGD/Input/Input Binding Settings")]
    public class InputBindingSettings : BindingTable<GameAction>, IRestoredAfterPlayMode
    {
        public List<ActionBinding> Bindings = new();

        protected override string SaveKey => "keybindings";

        public override int Count => Bindings.Count;

        public override GameAction ActionAt(int index) => Bindings[index].Action;

        public override string PathAt(int index, bool primary) =>
            primary ? Bindings[index].PrimaryPath : Bindings[index].SecondaryPath;

        public override void SetPath(int index, bool primary, string path)
        {
            ActionBinding binding = Bindings[index];
            if (primary) binding.PrimaryPath   = path;
            else         binding.SecondaryPath = path;
            Bindings[index] = binding;
        }

        public void SetMode(int index, InputActionMode mode)
        {
            ActionBinding binding = Bindings[index];
            binding.Mode    = mode;
            Bindings[index] = binding;
        }

        protected override string ModeAt(int index) => Bindings[index].Mode.ToString();

        protected override void SetMode(int index, string mode)
        {
            if (Enum.TryParse(mode, out InputActionMode parsed)) SetMode(index, parsed);
        }

        public bool TryGet(GameAction action, out ActionBinding binding)
        {
            int index = IndexOf(action);
            binding = index >= 0 ? Bindings[index] : default;
            return index >= 0;
        }

        [ContextMenu("Reset to Defaults")]
        public override void ResetToDefaults()
        {
            // Keyboard plus left/right mouse and the scroll wheel: extra mouse buttons aren't on every mouse.
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
                new() { Action = GameAction.NextWeapon,        PrimaryPath = "<Mouse>/scroll/down",  Mode = InputActionMode.Pressed },
                new() { Action = GameAction.PreviousWeapon,    PrimaryPath = "<Mouse>/scroll/up",    Mode = InputActionMode.Pressed },
                new() { Action = GameAction.LastWeapon,        PrimaryPath = "",                     Mode = InputActionMode.Pressed },
                new() { Action = GameAction.RoomGenerator,     PrimaryPath = "<Keyboard>/f2",        Mode = InputActionMode.Pressed },
            };
        }
    }
}
