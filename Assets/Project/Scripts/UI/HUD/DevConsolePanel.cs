using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using CGD.DevTools;
using CGD.Input;

namespace CGD.UI
{
    // The dev console window (` key): scrollback on top, a command line underneath.
    // Enter runs, Up/Down walk history, Tab completes a command name. Only opens where
    // DevCommands allows it (debug builds by default).
    public class DevConsolePanel : ModalPanel
    {
        private const int MaxLines = 60;

        [SerializeField] private DevCommands _commands;

        private readonly Queue<string> _lines = new();
        private readonly StringBuilder _text  = new();

        private TextMeshProUGUI _output;
        private TMP_InputField  _field;
        private int _historyIndex;

        protected override string  Title      => "Console";
        protected override Vector2 WindowSize => new(720f, 420f);

        protected override void Build(RectTransform content)
        {
            _output = UIFactory.MakeText("Output", content);
            _output.fontSize  = 13f;
            _output.alignment = TextAlignmentOptions.BottomLeft;
            _output.richText  = false;
            UIFactory.Stretch(_output.rectTransform);
            _output.rectTransform.offsetMin = new Vector2(0f, 34f);

            _field = UIFactory.MakeInputField("Input", content);
            RectTransform rt = _field.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, 28f);
            _field.onSubmit.AddListener(Submit);
            _field.onValueChanged.AddListener(StripToggleKey);

            Print("Type help for commands.");
        }

        private void Update()
        {
            if (_commands == null || !_commands.IsAvailable) return;

            if (_input != null && _input.WasPressedRaw(GameAction.Console) && !OpenedThisFrame)
            {
                if (IsVisible) Hide();
                else if (!IsBlocked(this)) Show();
                return;
            }

            if (IsVisible) HandleKeys();
        }

        public override void Refresh() { }

        protected override void OnOpened()
        {
            _historyIndex = _commands.Console.History.Count;
            Focus();
        }

        private void HandleKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.upArrowKey.wasPressedThisFrame)   StepHistory(-1);
            if (keyboard.downArrowKey.wasPressedThisFrame) StepHistory(1);
            if (keyboard.tabKey.wasPressedThisFrame)       Autocomplete();
        }

        private void Submit(string line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                Print("> " + line);
                string result = _commands.Console.Execute(line);
                if (!string.IsNullOrEmpty(result)) Print(result);
                _historyIndex = _commands.Console.History.Count;
            }

            _field.SetTextWithoutNotify(string.Empty);
            Focus();
        }

        private void StepHistory(int direction)
        {
            IReadOnlyList<string> history = _commands.Console.History;
            if (history.Count == 0) return;

            _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, history.Count);
            SetField(_historyIndex < history.Count ? history[_historyIndex] : string.Empty);
        }

        private void Autocomplete()
        {
            string typed = _field.text.Trim();
            if (typed.Contains(" ")) return;

            List<string> matches = _commands.Console.Complete(typed);
            if (matches.Count == 1) SetField(matches[0] + " ");
            else if (matches.Count > 1) Print(string.Join("  ", matches));
        }

        // The toggle key types a backquote into the field; drop it.
        private void StripToggleKey(string value)
        {
            if (value.IndexOf('`') >= 0) _field.SetTextWithoutNotify(value.Replace("`", ""));
        }

        private void SetField(string value)
        {
            _field.SetTextWithoutNotify(value);
            _field.caretPosition = value.Length;
        }

        private void Focus()
        {
            _field.Select();
            _field.ActivateInputField();
        }

        private void Print(string message)
        {
            foreach (string line in message.Split('\n'))
            {
                _lines.Enqueue(line);
                if (_lines.Count > MaxLines) _lines.Dequeue();
            }

            _text.Clear();
            foreach (string line in _lines) _text.Append(line).Append('\n');
            _output.text = _text.ToString();
        }
    }
}
