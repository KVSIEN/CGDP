using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CGD.Core;
using CGD.Factions;
using CGD.Input;
using CGD.Map;
using CGD.UI;

namespace CGD.Level
{
    // The room generator scene's controls, on a canvas of their own (the room generator key
    // opens and closes it, freeing the cursor like any window). Each row either pins a part of
    // the room or leaves it to the seed; Generate, New seed and Randomize all rebuild the room
    // through RoomGenerator.Apply, which reloads the scene. What the last room rolled to is
    // listed under the buttons and, while the panel is shut, in the corner.
    public sealed class RoomGeneratorPanel : MonoBehaviour
    {
        private const float Width = 440f;
        private const float Height = 600f;
        private const float Top = 48f;
        private const float RowHeight = 30f;
        private const float LabelWidth = 130f;
        private const float ControlX = 150f;
        private const float ControlWidth = 270f;

        private static readonly Color Background = new(0.07f, 0.08f, 0.12f, 0.96f);
        private static readonly Color Muted = new(0.6f, 0.63f, 0.7f, 1f);
        private static readonly Color DoorOn = new(0.25f, 0.5f, 0.9f, 0.95f);
        private static readonly Color DoorOff = new(0.16f, 0.16f, 0.22f, 0.95f);
        private static readonly string[] DoorNames = { "North", "East", "South", "West" };
        private static readonly string[] TierNames = { "Random", "1", "2", "3" };

        [SerializeField] private RoomGenerator _generator;
        [SerializeField] private PlayerInputHandler _input;
        [Tooltip("Only for the key shown in the corner hint")]
        [SerializeField] private InputBindingSettings _bindings;

        private readonly List<RectTransform> _rows = new();
        private readonly List<RoomFunction> _places = new();
        private readonly List<RoomLayout> _layouts = new();
        private readonly Image[] _doorButtons = new Image[DoorNames.Length];

        private RoomChoice _choice;
        private GameObject _root;
        private RectTransform _window;
        private RectTransform _breachRow;
        private TextMeshProUGUI _hint;
        private TextMeshProUGUI _summary;
        private TMP_InputField _seed;
        private OptionCycler _type, _category, _place, _layout, _faction, _breach, _section, _tier;
        private LabeledSlider _influence, _intensity;
        private int _openedFrame = -1;

        private bool IsOpen => _root != null && _root.activeSelf;

        private void Start()
        {
            if (_generator == null || _generator.Settings == null)
            {
                Debug.LogError($"{name}: RoomGeneratorPanel needs a RoomGenerator that has built its room.", this);
                enabled = false;
                return;
            }

            _choice = JsonUtility.FromJson<RoomChoice>(JsonUtility.ToJson(_generator.Choice));
            Build();
            RefreshAll();
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_input == null || !_input.WasPressedRaw(GameAction.RoomGenerator) || _openedFrame == Time.frameCount) return;
            if (!IsOpen && ModalPanel.Open != null) return;

            if (IsOpen) Close();
            else        Open();
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        private void Open()
        {
            _root.SetActive(true);
            _openedFrame = Time.frameCount;
            if (_input != null) _input.InputEnabled = false;
            CursorLock.Set(false);
            _hint.gameObject.SetActive(false);
        }

        private void Close()
        {
            _root.SetActive(false);
            if (_input != null)
            {
                _input.SetTextEntry(false);
                _input.InputEnabled = true;
            }
            CursorLock.Set(true);
            _hint.gameObject.SetActive(true);
        }

        // While the seed is being typed the other hotkeys stay quiet.
        private void SetTextEntry(bool active)
        {
            if (_input != null) _input.SetTextEntry(active);
        }

        private void Generate()
        {
            Close();
            _generator.Apply(_choice);
        }

        // --- Building the panel ----------------------------------------------------------

        private void Build()
        {
            var canvasObject = new GameObject("RoomGeneratorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRect = canvasObject.GetComponent<RectTransform>();

            _hint = UIFactory.MakeLabel("Hint", canvasRect, string.Empty, 14f, TextAlignmentOptions.TopLeft);
            UIFactory.Place(_hint.rectTransform, new Vector2(16f, -12f), new Vector2(900f, 48f));

            Image window = UIFactory.MakeImage("Window", canvasRect);
            window.color = Background;
            window.raycastTarget = true;
            _root = window.gameObject;
            _window = window.rectTransform;
            UIFactory.Pin(_window, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(Width, Height));

            TextMeshProUGUI title = UIFactory.MakeLabel("Title", _window, "Room Generator", 20f, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            UIFactory.Place(title.rectTransform, new Vector2(16f, -8f), new Vector2(Width - 80f, 30f));
            Button close = UIFactory.MakeButton("Close", _window, "X", out _);
            UIFactory.Place(close.GetComponent<RectTransform>(), new Vector2(Width - 44f, -10f), new Vector2(30f, 24f));
            close.onClick.AddListener(Close);

            _type     = AddCycler("Room type", OnTypeChanged);
            _category = AddCycler("Category", OnCategoryChanged);
            _place    = AddCycler("Place", OnPlaceChanged);
            _layout   = AddCycler("Layout", OnLayoutChanged);
            _faction  = AddCycler("Faction", i => _choice.Faction = FromFactionIndex(i));
            _breach   = AddCycler("Breached by", i => _choice.BreachFaction = i == 0 ? RoomChoice.Random : i - 1);
            _breachRow = _rows[_rows.Count - 1];
            _section  = AddCycler("Ship section", i => _choice.Section = FromFactionIndex(i));
            _tier     = AddCycler("Tier", i => _choice.Tier = i);
            _influence = AddSlider("Faction hold", v => _choice.Influence = v);
            _intensity = AddSlider("Intensity", v => _choice.Intensity = v);
            AddDoors();
            AddSeed();
            LayoutRows();

            AddButtons();
            _summary = UIFactory.MakeLabel("Summary", _window, string.Empty, 13f, TextAlignmentOptions.TopLeft);
            _summary.color = Muted;
            UIFactory.Place(_summary.rectTransform, new Vector2(16f, -Height + 100f), new Vector2(Width - 32f, 90f));
        }

        private RectTransform AddRow(string label)
        {
            var row = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(_window, false);
            _rows.Add(row);

            TextMeshProUGUI text = UIFactory.MakeLabel("Label", row, label, 14f, TextAlignmentOptions.MidlineLeft);
            text.color = Muted;
            UIFactory.Place(text.rectTransform, new Vector2(16f, 0f), new Vector2(LabelWidth, 20f));
            return row;
        }

        private OptionCycler AddCycler(string label, Action<int> onChanged) =>
            new(AddRow(label), new Vector2(ControlX, 0f), ControlWidth, Array.Empty<string>(), onChanged);

        private LabeledSlider AddSlider(string label, Action<float> onChanged) =>
            new(AddRow(label), new Vector2(ControlX, 0f), ControlWidth, 0f, 1f, v => v.ToString("0.00"), onChanged);

        private void AddDoors()
        {
            RectTransform row = AddRow("Doorways");
            const float ButtonWidth = 64f;
            for (int i = 0; i < DoorNames.Length; i++)
            {
                int side = i;
                Button button = UIFactory.MakeButton("Door " + DoorNames[i], row, DoorNames[i], out _);
                UIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(ControlX + i * (ButtonWidth + 4f), 0f), new Vector2(ButtonWidth, 22f));
                _doorButtons[i] = button.GetComponent<Image>();
                button.onClick.AddListener(() =>
                {
                    SetDoor(side, !Door(side));
                    ShowDoors();
                });
            }
        }

        private void AddSeed()
        {
            RectTransform row = AddRow("Seed");
            _seed = UIFactory.MakeInputField("Seed", row);
            UIFactory.Place(_seed.GetComponent<RectTransform>(), new Vector2(ControlX, 0f), new Vector2(ControlWidth, 22f));
            _seed.onSelect.AddListener(_ => SetTextEntry(true));
            _seed.onDeselect.AddListener(_ => SetTextEntry(false));
            _seed.onEndEdit.AddListener(text => _choice.SeedText = string.IsNullOrWhiteSpace(text) ? RoomChoice.NewSeedText() : text.Trim());
        }

        private void AddButtons()
        {
            const float ButtonWidth = 128f;
            float y = -Height + 140f;
            AddButton("Generate", y, 0, ButtonWidth, Generate);
            AddButton("New seed", y, 1, ButtonWidth, () =>
            {
                _choice.SeedText = RoomChoice.NewSeedText();
                Generate();
            });
            AddButton("Randomize all", y, 2, ButtonWidth, () =>
            {
                _choice.RandomizeAll();
                Generate();
            });
        }

        private void AddButton(string label, float y, int column, float width, UnityEngine.Events.UnityAction onClick)
        {
            Button button = UIFactory.MakeButton(label, _window, label, out TextMeshProUGUI text);
            text.fontSize = 15f;
            UIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(16f + column * (width + 12f), y), new Vector2(width, 32f));
            button.onClick.AddListener(onClick);
        }

        // Rows hang one under the other, the breach row only when the room can be one.
        private void LayoutRows()
        {
            float y = Top;
            foreach (RectTransform row in _rows)
            {
                if (!row.gameObject.activeSelf) continue;
                UIFactory.Place(row, new Vector2(0f, -y), new Vector2(Width, RowHeight));
                y += RowHeight;
            }
        }

        // --- Showing the choice ----------------------------------------------------------

        private void RefreshAll()
        {
            string[] types = Prepend("Random", Array.ConvertAll(RoomChoice.ChoosableTypes, t => t.ToString()));
            _type.SetOptions(types, _choice.RandomType ? 0 : Array.IndexOf(RoomChoice.ChoosableTypes, _choice.Type) + 1);

            string[] categories = Enum.GetNames(typeof(RoomCategory));
            categories[0] = "Any";
            _category.SetOptions(categories, (int)_choice.Category);

            string[] factions = FactionNames("Random", "None");
            _faction.SetOptions(factions, ToFactionIndex(_choice.Faction));
            _breach.SetOptions(Prepend("Random", FactionNames()), _choice.BreachFaction < 0 ? 0 : _choice.BreachFaction + 1);
            _section.SetOptions(Prepend("Random", Prepend("None", SectionNames())), ToFactionIndex(_choice.Section));
            _tier.SetOptions(TierNames, Mathf.Clamp(_choice.Tier, 0, 3));
            _influence.SetValueWithoutNotify(_choice.Influence);
            _intensity.SetValueWithoutNotify(_choice.Intensity);
            _seed.SetTextWithoutNotify(_choice.SeedText);

            RefreshPlaces();
            ShowBreachRow();
            ShowDoors();
            _hint.text = Hint();
            _summary.text = Summary();
        }

        // Only the places of the chosen category are listed; a pinned place the category rules out is let go.
        private void RefreshPlaces()
        {
            _places.Clear();
            foreach (RoomFunction function in _generator.Settings.Functions)
                if (function != null && (_choice.Category == RoomCategory.None || function.Category == _choice.Category))
                    _places.Add(function);
            _places.Sort((a, b) => string.Compare(NameOf(a), NameOf(b), StringComparison.OrdinalIgnoreCase));

            int selected = _places.FindIndex(f => f.name == _choice.Place);
            if (selected < 0) _choice.Place = _choice.Layout = string.Empty;
            _place.SetOptions(Prepend("Any", _places.ConvertAll(NameOf).ToArray()), selected + 1);
            RefreshLayouts();
        }

        private void RefreshLayouts()
        {
            _layouts.Clear();
            RoomFunction place = _choice.FindPlace(_generator.Settings);
            if (place != null)
                foreach (RoomLayout layout in place.Layouts)
                    if (layout != null) _layouts.Add(layout);

            int selected = _layouts.FindIndex(l => l.name == _choice.Layout);
            if (selected < 0) _choice.Layout = string.Empty;
            _layout.SetOptions(Prepend("Any", _layouts.ConvertAll(l => string.IsNullOrEmpty(l.DisplayName) ? l.name : l.DisplayName).ToArray()), selected + 1);
        }

        private void ShowBreachRow()
        {
            _breachRow.gameObject.SetActive(!_choice.RandomType && _choice.Type.HasSecondFaction());
            LayoutRows();
        }

        private void ShowDoors()
        {
            for (int i = 0; i < _doorButtons.Length; i++)
                _doorButtons[i].color = Door(i) ? DoorOn : DoorOff;
        }

        // --- Changes ---------------------------------------------------------------------

        private void OnTypeChanged(int index)
        {
            _choice.RandomType = index == 0;
            if (index > 0) _choice.Type = RoomChoice.ChoosableTypes[index - 1];
            ShowBreachRow();
        }

        private void OnCategoryChanged(int index)
        {
            _choice.Category = (RoomCategory)index;
            RefreshPlaces();
        }

        private void OnPlaceChanged(int index)
        {
            _choice.Place = index == 0 ? string.Empty : _places[index - 1].name;
            _choice.Layout = string.Empty;
            RefreshLayouts();
        }

        private void OnLayoutChanged(int index) =>
            _choice.Layout = index == 0 ? string.Empty : _layouts[index - 1].name;

        private bool Door(int side) => side switch
        {
            0 => _choice.DoorNorth,
            1 => _choice.DoorEast,
            2 => _choice.DoorSouth,
            _ => _choice.DoorWest,
        };

        private void SetDoor(int side, bool open)
        {
            switch (side)
            {
                case 0: _choice.DoorNorth = open; break;
                case 1: _choice.DoorEast = open; break;
                case 2: _choice.DoorSouth = open; break;
                default: _choice.DoorWest = open; break;
            }
        }

        // --- Words -----------------------------------------------------------------------

        // Random and None come before the factions and sections, so their indices run two ahead.
        private static int ToFactionIndex(int value) => value == RoomChoice.Random ? 0 : value == RoomChoice.None ? 1 : value + 2;

        private static int FromFactionIndex(int index) => index == 0 ? RoomChoice.Random : index == 1 ? RoomChoice.None : index - 2;

        private static string NameOf(RoomFunction function) =>
            string.IsNullOrEmpty(function.DisplayName) ? function.name : function.DisplayName;

        private static string[] Prepend(string first, string[] rest)
        {
            var all = new string[rest.Length + 1];
            all[0] = first;
            Array.Copy(rest, 0, all, 1, rest.Length);
            return all;
        }

        private string[] FactionNames(params string[] leading)
        {
            var names = new List<string>(leading);
            if (_generator.Content != null)
                for (int i = 0; i < _generator.Content.Factions.Count; i++) names.Add(FactionName(i));
            return names.ToArray();
        }

        private string[] SectionNames()
        {
            var names = new List<string>();
            if (_generator.Content != null)
                for (int i = 0; i < _generator.Content.Sections.Count; i++) names.Add(SectionName(i));
            return names.ToArray();
        }

        private string FactionName(int index)
        {
            if (_generator.Content == null || index < 0 || index >= _generator.Content.Factions.Count) return "none";
            FactionDefinition faction = _generator.Content.Factions[index];
            return faction != null ? faction.DisplayName : $"Faction {index}";
        }

        private string SectionName(int index)
        {
            if (_generator.Content == null || index < 0 || index >= _generator.Content.Sections.Count) return "none";
            MapSectionDefinition section = _generator.Content.Sections[index];
            return section != null ? section.DisplayName : $"Section {index}";
        }

        // What the room being built rolled to: the choices left to the seed, filled in.
        private string Summary()
        {
            RoomRecipe room = _generator.Recipe;
            if (room == null) return string.Empty;

            string breach = room.Type.HasSecondFaction() ? $"  ·  breached by {FactionName(room.BreachFaction)}" : string.Empty;
            string place = room.Place != null ? NameOf(room.Place) : room.Category != RoomCategory.None ? $"any {room.Category} place" : "any place";
            return $"Now in the room: {room.Type}, {place}\n" +
                   $"Held by {FactionName(room.Faction)}{breach}  ·  {SectionName(room.Section)} section\n" +
                   $"Tier {room.Tier}  ·  hold {room.Influence:0.00}  ·  intensity {room.Intensity:0.00}  ·  seed {_generator.Choice.SeedText}";
        }

        private string Hint()
        {
            RoomRecipe room = _generator.Recipe;
            string key = _bindings != null ? _bindings.PromptFor(GameAction.RoomGenerator) : InputPromptLabels.Unbound;
            return room == null ? $"[{key}] Room generator" : $"[{key}] Room generator  ·  {room.Type}, tier {room.Tier}, seed {_generator.Choice.SeedText}";
        }
    }
}
