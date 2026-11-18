using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Editor
{
    // CGD › Weapons › Firearm Lab: where new firearm models are made and tuned. Pick a weapon
    // category; its models are listed with the odds of each being rolled. Add one from an
    // archetype, edit it (weight, stat scales, firing variants, recoil), and the table below
    // re-rolls the category the way the game does, so the effect of every change shows in the
    // guns it actually produces — damage, fire rate, time to kill, recoil, spread.
    internal sealed class FirearmLabWindow : EditorWindow
    {
        private static readonly FirearmStat[] TableStats =
        {
            FirearmStat.Damage, FirearmStat.FireRate, FirearmStat.Dps, FirearmStat.TimeToKill,
            FirearmStat.Magazine, FirearmStat.Reload, FirearmStat.RecoilVertical, FirearmStat.HipSpread,
        };

        private const int ModelColumnWidth = 150;
        private const int StatColumnWidth  = 62;

        [SerializeField] private WeaponCategoryData _category;
        [SerializeField] private ItemTier _tier = ItemTier.Rare;
        [SerializeField] private int      _sampleCount = 400;
        [SerializeField] private int      _selected;
        [SerializeField] private string   _seedText = "1";

        private SerializedObject      _serialized;
        private List<FirearmSample>   _samples = new();
        private bool                  _stale = true;
        private Vector2               _scroll;

        [MenuItem("CGD/Weapons/Firearm Lab…")]
        public static void OpenWindow() => Open(null);

        public static void Open(WeaponCategoryData category)
        {
            var window = GetWindow<FirearmLabWindow>("Firearm Lab");
            window.minSize = new Vector2(620f, 520f);
            if (category != null) window.SetCategory(category);
            window.Show();
        }

        private void OnEnable() => Undo.undoRedoPerformed += MarkStale;
        private void OnDisable() => Undo.undoRedoPerformed -= MarkStale;

        private void MarkStale()
        {
            _stale = true;
            Repaint();
        }

        private void SetCategory(WeaponCategoryData category)
        {
            _category   = category;
            _serialized = category != null ? new SerializedObject(category) : null;
            _selected   = 0;
            _stale      = true;
        }

        private void OnGUI()
        {
            DrawHeader();
            if (_category == null)
            {
                EditorGUILayout.HelpBox("Pick a weapon category (Data/Weapons/Categories) to see and edit its models.", MessageType.Info);
                return;
            }
            if (_serialized == null || _serialized.targetObject != _category) _serialized = new SerializedObject(_category);
            _serialized.Update();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUI.BeginChangeCheck();
            DrawModels();
            DrawSelectedModel();
            if (EditorGUI.EndChangeCheck())
            {
                _serialized.ApplyModifiedProperties();
                _stale = true;
            }

            if (_stale) Resample();
            DrawComparison();
            DrawSelectedBreakdown();
            EditorGUILayout.EndScrollView();
        }

        // ── header ────────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUI.BeginChangeCheck();
            var category = (WeaponCategoryData)EditorGUILayout.ObjectField("Category", _category, typeof(WeaponCategoryData), false);
            if (category != _category) SetCategory(category);

            using (new EditorGUILayout.HorizontalScope())
            {
                _tier        = (ItemTier)EditorGUILayout.EnumPopup(new GUIContent("Roll at tier", "The quality tier the sample guns are rolled at"), _tier);
                _sampleCount = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Samples", "How many guns to roll; more is steadier but slower"), _sampleCount), 20, 5000);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _seedText = EditorGUILayout.TextField(new GUIContent("Seed", "The same seed rolls the same guns, so changes to the models compare like for like"), _seedText);
                if (GUILayout.Button("New seed", GUILayout.Width(80f)))
                    _seedText = UnityEngine.Random.Range(1, int.MaxValue).ToString();
            }
            if (EditorGUI.EndChangeCheck()) _stale = true;
        }

        // ── models and their odds ─────────────────────────────────────────────────

        private void DrawModels()
        {
            WeaponModel[] models = _category.Models;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Models", EditorStyles.boldLabel);

            if (models.Length == 0)
                EditorGUILayout.HelpBox("This category has no models, so it rolls generic weapons from its Names and Fire Modes. Add a model to make it roll real guns.", MessageType.None);

            float totalWeight = 0f;
            foreach (WeaponModel model in models) totalWeight += Mathf.Max(0f, model.Weight);

            SerializedProperty list = _serialized.FindProperty("Models");
            for (int i = 0; i < models.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool selected = i == _selected;
                    if (GUILayout.Toggle(selected, string.IsNullOrEmpty(models[i].Name) ? "(unnamed)" : models[i].Name, "Button", GUILayout.Width(ModelColumnWidth)) && !selected)
                        _selected = i;

                    SerializedProperty weight = list.GetArrayElementAtIndex(i).FindPropertyRelative("Weight");
                    EditorGUILayout.PropertyField(weight, GUIContent.none, GUILayout.Width(50f));
                    float odds = totalWeight > 0f ? Mathf.Max(0f, models[i].Weight) / totalWeight : 0f;
                    EditorGUILayout.LabelField($"{odds:P0} of rolls", GUILayout.Width(90f));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add model ▾", GUILayout.Width(110f))) ShowAddMenu();
                using (new EditorGUI.DisabledScope(models.Length == 0))
                {
                    if (GUILayout.Button("Duplicate", GUILayout.Width(80f))) Edit("Duplicate Firearm Model", () => Insert(FirearmTemplates.Copy(models[_selected]), "{0} Copy"));
                    if (GUILayout.Button("Remove", GUILayout.Width(80f)) && ConfirmRemove(models[_selected])) Edit("Remove Firearm Model", RemoveSelected);
                }
            }
        }

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (FirearmTemplates.Archetype archetype in FirearmTemplates.Archetypes)
            {
                FirearmTemplates.Archetype captured = archetype;
                menu.AddItem(new GUIContent(archetype.Name), false, () => Edit("Add Firearm Model", () => Insert(captured.Create(), "{0}")));
            }
            menu.ShowAsContext();
        }

        private bool ConfirmRemove(WeaponModel model) =>
            EditorUtility.DisplayDialog("Remove model", $"Remove \"{model.Name}\" from {_category.name}? Undo brings it back.", "Remove", "Cancel");

        // One undoable change to the category's model list. Deferred, so the list never changes
        // in the middle of drawing it.
        private void Edit(string label, Action change)
        {
            WeaponCategoryData category = _category;
            EditorApplication.delayCall += () =>
            {
                if (category == null) return;
                Undo.RecordObject(category, label);
                change();
                EditorUtility.SetDirty(category);
                _serialized = null;
                _stale      = true;
                Repaint();
            };
        }

        private void Insert(WeaponModel model, string nameFormat)
        {
            model.Name = string.Format(nameFormat, model.Name);
            var models = new List<WeaponModel>(_category.Models) { model };
            _category.Models = models.ToArray();
            _selected = models.Count - 1;
        }

        private void RemoveSelected()
        {
            var models = new List<WeaponModel>(_category.Models);
            models.RemoveAt(_selected);
            _category.Models = models.ToArray();
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, models.Count - 1));
        }

        // ── the selected model ────────────────────────────────────────────────────

        private void DrawSelectedModel()
        {
            if (_category.Models.Length == 0) return;
            _selected = Mathf.Clamp(_selected, 0, _category.Models.Length - 1);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Selected model", EditorStyles.boldLabel);
                if (GUILayout.Button("Recoil feel ▾", GUILayout.Width(110f))) ShowRecoilMenu();
            }

            SerializedProperty model = _serialized.FindProperty("Models").GetArrayElementAtIndex(_selected);
            EditorGUILayout.HelpBox(
                "Scales stretch the category's stat ranges for this model (1 = the category's middle) and tilt its rolls toward that side. " +
                "Firing Styles are its variants, picked by weight. Recoil shapes how it kicks.", MessageType.None);
            EditorGUILayout.PropertyField(model, true);
        }

        private void ShowRecoilMenu()
        {
            var menu = new GenericMenu();
            foreach (FirearmTemplates.RecoilPreset preset in FirearmTemplates.RecoilPresets)
            {
                FirearmTemplates.RecoilPreset captured = preset;
                menu.AddItem(new GUIContent(preset.Name), false, () =>
                    Edit("Set Recoil Feel", () => _category.Models[_selected].Recoil = FirearmTemplates.Copy(captured.Pattern)));
            }
            menu.ShowAsContext();
        }

        // ── what the rolls produce ────────────────────────────────────────────────

        private void Resample()
        {
            _stale = false;
            _samples = _category != null
                ? FirearmSampler.Roll(_category, _tier, _sampleCount, CGD.Core.Seed.Parse(_seedText).Value)
                : new List<FirearmSample>();
        }

        private void DrawComparison()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Average of {_samples.Count} guns rolled at {_tier}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Time to kill is for {FirearmSampler.TargetHealth:0} health with every shot landing; damage counts every pellet.", EditorStyles.wordWrappedMiniLabel);

            DrawRow("Model", s => StatName(s), null, true);
            DrawRow("All", null, _samples, false);
            WeaponModel[] models = _category.Models;
            for (int i = 0; i < models.Length; i++)
                DrawRow(models[i].Name, null, FirearmSampler.Of(_samples, models[i]), false, i == _selected);
        }

        private static string StatName(FirearmStat stat) => stat switch
        {
            FirearmStat.Damage         => "Dmg/shot",
            FirearmStat.FireRate       => "RPM",
            FirearmStat.Dps            => "DPS",
            FirearmStat.TimeToKill     => "TTK s",
            FirearmStat.Magazine       => "Mag",
            FirearmStat.Reload         => "Reload",
            FirearmStat.RecoilVertical => "Kick °",
            FirearmStat.HipSpread      => "Hip °",
            _                          => stat.ToString(),
        };

        private static void DrawRow(string label, Func<FirearmStat, string> header, IReadOnlyList<FirearmSample> rows, bool isHeader, bool highlight = false)
        {
            using (new EditorGUILayout.HorizontalScope(highlight ? EditorStyles.helpBox : GUIStyle.none))
            {
                GUILayout.Label(label, isHeader ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(ModelColumnWidth));
                foreach (FirearmStat stat in TableStats)
                {
                    string text = isHeader ? header(stat)
                                : rows.Count == 0 ? "–"
                                : Format(stat, FirearmSampler.Spread(rows, stat).Average);
                    GUILayout.Label(text, isHeader ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(StatColumnWidth));
                }
            }
        }

        private static string Format(FirearmStat stat, float value) => stat switch
        {
            FirearmStat.FireRate or FirearmStat.Dps or FirearmStat.Magazine => value.ToString("0"),
            FirearmStat.TimeToKill => float.IsInfinity(value) ? "∞" : value.ToString("0.00"),
            _ => value.ToString("0.0#"),
        };

        private void DrawSelectedBreakdown()
        {
            if (_category.Models.Length == 0) return;
            WeaponModel model = _category.Models[_selected];
            List<FirearmSample> own = FirearmSampler.Of(_samples, model);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{model.Name}: range of its {own.Count} rolls", EditorStyles.boldLabel);
            if (own.Count == 0)
            {
                EditorGUILayout.HelpBox("None of the sample guns came from this model (its weight may be 0). Raise Samples or its weight.", MessageType.None);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Stat", EditorStyles.boldLabel, GUILayout.Width(ModelColumnWidth));
                foreach (string title in new[] { "Min", "Average", "Max" })
                    GUILayout.Label(title, EditorStyles.boldLabel, GUILayout.Width(StatColumnWidth));
            }
            foreach (FirearmStat stat in Enum.GetValues(typeof(FirearmStat)))
            {
                StatSpread spread = FirearmSampler.Spread(own, stat);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(stat.ToString(), GUILayout.Width(ModelColumnWidth));
                    GUILayout.Label(Format(stat, spread.Min),     GUILayout.Width(StatColumnWidth));
                    GUILayout.Label(Format(stat, spread.Average), GUILayout.Width(StatColumnWidth));
                    GUILayout.Label(Format(stat, spread.Max),     GUILayout.Width(StatColumnWidth));
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Guns it rolls, by share (variant and stat style in the name)", EditorStyles.boldLabel);
            foreach (KeyValuePair<string, float> share in FirearmSampler.NameShares(own))
                EditorGUILayout.LabelField($"{share.Value:P0}  {share.Key}");
        }
    }
}
