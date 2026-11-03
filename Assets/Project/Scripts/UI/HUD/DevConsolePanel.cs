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
    // Enter runs, Up/Down walk history. A hint line shows the command being typed and the
    // options for its current argument; Tab fills in the shared start of those options, and
    // pressing it again cycles through them. Only opens where DevCommands allows it (debug
    // builds by default).
    public class DevConsolePanel : ModalPanel
    {
        private const int   MaxLines       = 60;
        private const int   MaxHintOptions = 8;
        private const float HintHeight     = 38f;
        private const float InputHeight    = 28f;

        [SerializeField] private DevCommands _commands;

        private readonly Queue<string> _lines = new();
        private readonly StringBuilder _text  = new();

        private TextMeshProUGUI _output;
        private TextMeshProUGUI _hint;
        private TMP_InputField  _field;
        private int _historyIndex;

        // Set while Tab is cycling through one word's options, so the list doesn't shrink as it fills in.
        private Completion _cycle;
        private int        _cycleIndex;
        private string     _cycleText;

        protected override string  Title      => "Console";
        protected override Vector2 WindowSize => new(720f, 420f);

        protected override void Build(RectTransform content)
        {
            _output = UIFactory.MakeText("Output", content);
            _output.fontSize  = 13f;
            _output.alignment = TextAlignmentOptions.BottomLeft;
            _output.richText  = false;
            UIFactory.Stretch(_output.rectTransform);
            _output.rectTransform.offsetMin = new Vector2(0f, InputHeight + HintHeight + 6f);

            _hint = UIFactory.MakeText("Hint", content);
            _hint.fontSize         = 12f;
            _hint.alignment        = TextAlignmentOptions.BottomLeft;
            _hint.color            = new Color(0.62f, 0.76f, 0.95f);
            _hint.richText         = false;
            _hint.overflowMode     = TextOverflowModes.Truncate;
            RectTransform hint     = _hint.rectTransform;
            hint.anchorMin         = Vector2.zero;
            hint.anchorMax         = new Vector2(1f, 0f);
            hint.pivot             = new Vector2(0.5f, 0f);
            hint.anchoredPosition  = new Vector2(0f, InputHeight + 4f);
            hint.sizeDelta         = new Vector2(0f, HintHeight);

            _field = UIFactory.MakeInputField("Input", content);
            RectTransform rt = _field.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, InputHeight);
            _field.onSubmit.AddListener(Submit);
            _field.onValueChanged.AddListener(StripToggleKey);
            _field.onValueChanged.AddListener(OnTyped);

            Print("Type /help for commands. Tab completes names — try /weapon, then Tab.");
            RefreshHint(string.Empty);
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

        protected override void OnClosed() => _input?.SetTextEntry(false);

        protected override void OnOpened()
        {
            _historyIndex = _commands.Console.History.Count;
            _input?.SetTextEntry(true);
            RefreshHint(_field.text);
            Focus();
        }

        private void HandleKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)    { Hide(); return; }
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

            SetField(string.Empty);
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
            if (_cycle == null && !StartCompleting()) return;

            _cycleIndex = (_cycleIndex + 1) % _cycle.Candidates.Count;
            _cycleText  = _cycle.Apply(_cycleIndex);
            SetField(_cycleText);
        }

        // A single option, or a longer shared start, is filled in at once. Only when neither
        // helps does Tab begin cycling. Returns whether a cycle began.
        private bool StartCompleting()
        {
            Completion completion = _commands.Console.Complete(_field.text);
            if (completion.Candidates.Count == 0) return false;

            if (completion.Candidates.Count == 1)
            {
                SetField(completion.Apply(0) + " ");
                return false;
            }

            string common = completion.CommonPrefix;
            if (common.Length > completion.Typed.Length)
            {
                SetField(completion.Line.Substring(0, completion.TokenStart) + common);
                return false;
            }

            _cycle      = completion;
            _cycleIndex = -1;
            return true;
        }

        private void OnTyped(string value)
        {
            if (_cycle != null && value != _cycleText) _cycle = null;
            RefreshHint(value);
        }

        // What the player is typing and what could come next: the command's usage on top,
        // the options for the current word underneath (the cycled-to one in brackets).
        private void RefreshHint(string text)
        {
            Completion completion = _cycle ?? _commands.Console.Complete(text);

            var hint = new StringBuilder();
            if (completion.Signature != null)
                hint.Append(completion.Signature).Append('\n');
            else if (string.IsNullOrEmpty(text))
                hint.Append("Tab completes  ·  Up/Down history  ·  /help lists commands\n");

            int shown = Mathf.Min(completion.Candidates.Count, MaxHintOptions);
            for (int i = 0; i < shown; i++)
            {
                string option = completion.Candidates[i];
                hint.Append(_cycle != null && i == _cycleIndex ? $"[{option}]" : option).Append("  ");
            }
            if (completion.Candidates.Count > shown) hint.Append($"… +{completion.Candidates.Count - shown} more");

            if (completion.Candidates.Count == 0 && completion.Signature == null && !string.IsNullOrEmpty(text))
                hint.Append(text[0] == DevConsole.CommandPrefix ? "No such command  —  /help lists them" : "Commands start with /");

            _hint.text = hint.ToString().TrimEnd();
        }

        // The toggle key types a backquote into the field; drop it.
        private void StripToggleKey(string value)
        {
            if (value.IndexOf('`') >= 0) _field.SetTextWithoutNotify(value.Replace("`", ""));
        }

        private void SetField(string value)
        {
            if (value != _cycleText) _cycle = null;

            _field.SetTextWithoutNotify(value);
            _field.caretPosition = value.Length;
            RefreshHint(value);
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
