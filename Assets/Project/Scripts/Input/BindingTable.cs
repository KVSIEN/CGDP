using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Input
{
    // A rebindable control table: every TAction has a primary and a secondary Input System
    // path ("<Keyboard>/g", "<Mouse>/leftButton"; empty = unbound). Holds what every table
    // shares — lookups, key prompts, shared-control checks and PlayerPrefs storage under the
    // table's own key — so a table only declares its slots and its defaults. Used by the
    // player's InputBindingSettings and the spectator's SpectatorInputSettings; rebinding
    // goes through BindingRebinder.
    public abstract class BindingTable<TAction> : ScriptableObject where TAction : struct, Enum
    {
        // Raised after the bindings were loaded, rebound or reset, so key hints can redraw.
        // Listeners unsubscribe in OnDisable.
        public event Action Changed;

        public void NotifyChanged() => Changed?.Invoke();

        public abstract int Count { get; }
        public abstract TAction ActionAt(int index);
        public abstract string PathAt(int index, bool primary);
        public abstract void SetPath(int index, bool primary, string path);
        public abstract void ResetToDefaults();

        // PlayerPrefs key the rebound controls are saved under; each table has its own.
        protected abstract string SaveKey { get; }

        // False for a row the player may not change (it stays on its default).
        public virtual bool IsRebindable(TAction action) => true;

        // Name an action is called by in messages ("Space is also bound to Jump").
        public virtual string LabelOf(TAction action) => action.ToString();

        // How the action fires, for tables whose rows have a mode (null = none); saved by name.
        protected virtual string ModeAt(int index) => null;
        protected virtual void SetMode(int index, string mode) { }

        public int IndexOf(TAction action)
        {
            for (int i = 0; i < Count; i++)
                if (EqualityComparer<TAction>.Default.Equals(ActionAt(i), action)) return i;
            return -1;
        }

        // The short name of the control shown for an action: its primary binding, else the
        // secondary one.
        public string PromptFor(TAction action)
        {
            int index = IndexOf(action);
            if (index < 0) return InputPromptLabels.Unbound;

            string primary = PathAt(index, true);
            return InputPromptLabels.Short(string.IsNullOrEmpty(primary) ? PathAt(index, false) : primary);
        }

        // Comma-separated actions that use this control in a slot other than the one given (so a
        // slot is never shared with itself); empty when the control is unshared.
        public string SharedWith(TAction action, bool primary, string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var names = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                bool sameAction = EqualityComparer<TAction>.Default.Equals(ActionAt(i), action);
                bool shared = (!(sameAction && primary)  && PathAt(i, true)  == path)
                           || (!(sameAction && !primary) && PathAt(i, false) == path);
                if (shared) names.Add(LabelOf(ActionAt(i)));
            }
            return string.Join(", ", names);
        }

        public void Save()
        {
            var data = new BindingSaveData { entries = new BindingSaveData.Entry[Count] };
            for (int i = 0; i < Count; i++)
            {
                data.entries[i] = new BindingSaveData.Entry
                {
                    action        = ActionAt(i).ToString(),
                    primaryPath   = PathAt(i, true),
                    secondaryPath = PathAt(i, false),
                    mode          = ModeAt(i),
                };
            }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        // Applies saved controls over the asset's (in memory only). False if none were saved.
        public bool LoadSaved()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            var data = JsonUtility.FromJson<BindingSaveData>(PlayerPrefs.GetString(SaveKey));
            if (data?.entries == null) return false;

            foreach (BindingSaveData.Entry entry in data.entries)
            {
                if (!Enum.TryParse(entry.action, out TAction action)) continue;
                int index = IndexOf(action);
                if (index < 0 || !IsRebindable(action)) continue;

                SetPath(index, true,  entry.primaryPath);
                SetPath(index, false, entry.secondaryPath);
                if (!string.IsNullOrEmpty(entry.mode)) SetMode(index, entry.mode);
            }
            return true;
        }

        [ContextMenu("Clear Saved Bindings (PlayerPrefs)")]
        public void DeleteSaved() => PlayerPrefs.DeleteKey(SaveKey);
    }
}
