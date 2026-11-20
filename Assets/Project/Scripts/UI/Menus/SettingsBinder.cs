using System;
using System.Collections.Generic;
using CGD.Settings;

namespace CGD.UI
{
    // Ties a settings page's rows to fields of the draft, so each option is declared once: the
    // row it shows, where its value lives and how to write it. From that the binder shows every
    // row when a draft is loaded, writes edits into the draft and reports them, and puts every
    // row back to its default. Rows lay out through the page's SettingsPageLayout; a page with
    // several columns binds the others with On(column), and they all load and reset together.
    public sealed class SettingsBinder
    {
        private readonly SettingsBinder _page;   // the binder that holds the rows and the draft (itself, or the one On was called on)
        private readonly SettingsPageLayout _layout;
        private readonly Action _changed;
        private readonly List<Action<SettingsData>> _show = new();
        private readonly List<Action<SettingsData, SettingsData>> _reset = new();
        private SettingsData _draft;

        public SettingsBinder(SettingsPageLayout layout, Action changed)
        {
            _page    = this;
            _layout  = layout;
            _changed = changed;
        }

        private SettingsBinder(SettingsBinder page, SettingsPageLayout layout)
        {
            _page   = page;
            _layout = layout;
        }

        public SettingsData Draft => _page._draft;

        // The same page's rows, laid out down another column.
        public SettingsBinder On(SettingsPageLayout column) => new(_page, column);

        public void Slider(string label, float min, float max, Func<float, string> format,
            Func<SettingsData, float> get, Action<SettingsData, float> set)
        {
            LabeledSlider slider = _layout.Slider(label, min, max, format, v => Change(d => set(d, v)));
            Bind(d => slider.SetValueWithoutNotify(get(d)), (defaults, draft) => set(draft, get(defaults)));
        }

        public void Toggle(string label, Func<SettingsData, bool> get, Action<SettingsData, bool> set)
        {
            OptionCycler toggle = _layout.Toggle(label, on => Change(d => set(d, on)));
            Bind(d => toggle.SetIndexWithoutNotify(SettingsPageLayout.ToggleIndex(get(d))),
                 (defaults, draft) => set(draft, get(defaults)));
        }

        // One of a fixed list of values, shown by the label at the same index.
        public void Options<T>(string label, string[] labels, T[] values, Func<SettingsData, T> get, Action<SettingsData, T> set)
        {
            OptionCycler options = _layout.Options(label, labels, i => Change(d => set(d, values[i])));
            Bind(d => options.SetIndexWithoutNotify(Array.IndexOf(values, get(d))),
                 (defaults, draft) => set(draft, get(defaults)));
        }

        // Every value of an enum, labelled by name.
        public void Options<TEnum>(string label, Func<SettingsData, TEnum> get, Action<SettingsData, TEnum> set)
            where TEnum : struct, Enum =>
            Options(label, Enum.GetNames(typeof(TEnum)), (TEnum[])Enum.GetValues(typeof(TEnum)), get, set);

        // For a row the helpers above don't cover: how it shows a draft and how it resets
        // (null = it keeps its value when the page is reset).
        public void Bind(Action<SettingsData> show, Action<SettingsData, SettingsData> reset)
        {
            _page._show.Add(show);
            if (reset != null) _page._reset.Add(reset);
        }

        public void Change(Action<SettingsData> change)
        {
            change(Draft);
            _page._changed();
        }

        public void Load(SettingsData draft)
        {
            _page._draft = draft;
            foreach (Action<SettingsData> show in _page._show) show(draft);
        }

        public void ResetToDefaults()
        {
            var defaults = new SettingsData();
            foreach (Action<SettingsData, SettingsData> reset in _page._reset) reset(defaults, Draft);
            Load(Draft);
            _page._changed();
        }
    }
}
