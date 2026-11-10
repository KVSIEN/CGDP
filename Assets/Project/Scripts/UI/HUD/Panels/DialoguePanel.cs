using TMPro;
using UnityEngine;
using CGD.Dialogue;
using CGD.Input;

namespace CGD.UI
{
    // Shows the conversation an Npc starts: who's speaking, their line, and the player's
    // replies as buttons. The first four replies can also be picked with the weapon-slot
    // keys (1–4 by default). Closing the window ends the conversation.
    public class DialoguePanel : ModalPanel
    {
        private const float LineHeight = 120f;

        private static readonly GameAction[] ChoiceKeys =
            { GameAction.Weapon1, GameAction.Weapon2, GameAction.Weapon3, GameAction.Weapon4 };

        private TextMeshProUGUI _line;
        private UIButtonList    _choices;
        private Conversation    _conversation;

        protected override string  Title      => "Dialogue";
        protected override Vector2 WindowSize => new(620f, 340f);

        protected override void Build(RectTransform content)
        {
            _line = UIFactory.MakeText("Line", content);
            _line.fontSize  = 15f;
            _line.alignment = TextAlignmentOptions.TopLeft;
            RectTransform lineRt = _line.rectTransform;
            lineRt.anchorMin = new Vector2(0f, 1f);
            lineRt.anchorMax = Vector2.one;
            lineRt.pivot     = new Vector2(0.5f, 1f);
            lineRt.offsetMin = new Vector2(4f, -LineHeight);
            lineRt.offsetMax = new Vector2(-4f, 0f);

            var choicesRoot = new GameObject("Choices", typeof(RectTransform)).GetComponent<RectTransform>();
            choicesRoot.SetParent(content, false);
            UIFactory.Stretch(choicesRoot);
            choicesRoot.offsetMax = new Vector2(0f, -LineHeight - 8f);
            _choices = new UIButtonList(choicesRoot, 28f);
        }

        private void OnEnable() => Npc.ConversationStarted += OnConversationStarted;

        protected override void OnDisable()
        {
            Npc.ConversationStarted -= OnConversationStarted;
            base.OnDisable();
        }

        private void Update()
        {
            if (!IsVisible || OpenedThisFrame || _conversation == null || _input == null) return;

            int count = Mathf.Min(ChoiceKeys.Length, _conversation.Choices.Count);
            for (int i = 0; i < count; i++)
            {
                if (!_input.WasPressedRaw(ChoiceKeys[i])) continue;
                _conversation.Choose(i);
                return;
            }
        }

        private void OnConversationStarted(Conversation conversation)
        {
            if (IsBlocked(this)) return;

            Detach();
            _conversation = conversation;
            _conversation.Changed += Refresh;
            _conversation.Ended   += Hide;
            Show();
        }

        protected override void OnOpened()
        {
            SetTitle(_conversation.Context.SpeakerName);
            Refresh();
        }

        // Closing the window (X) walks away mid-conversation.
        protected override void OnClosed()
        {
            Conversation conversation = _conversation;
            Detach();
            conversation?.End();
        }

        public override void Refresh()
        {
            // Current is still null when the panel opens: the Npc raises ConversationStarted
            // before Start(), and Start's Changed event refreshes again with the first line.
            if (!IsVisible || _conversation?.Current == null) return;

            _line.text = _conversation.Current.Text;

            _choices.Begin();
            for (int i = 0; i < _conversation.Choices.Count; i++)
            {
                int index = i;
                string key = i < ChoiceKeys.Length ? $"{i + 1}.  " : string.Empty;
                _choices.Add(key + _conversation.Choices[i].Text, () => _conversation?.Choose(index));
            }

            // A line with nothing to say back still needs a way out.
            if (_conversation.Choices.Count == 0) _choices.Add("Leave", () => _conversation?.End());
            _choices.End();
        }

        private void Detach()
        {
            if (_conversation == null) return;

            _conversation.Changed -= Refresh;
            _conversation.Ended   -= Hide;
            _conversation = null;
        }
    }
}
