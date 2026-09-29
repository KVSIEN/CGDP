using System;
using System.Collections.Generic;

namespace CGD.Dialogue
{
    // Walks a DialogueDefinition: the current line, the choices the player may pick right
    // now (conditions applied), and what picking one does. The UI only displays this and
    // forwards clicks, so any front end (dialogue box, subtitles, a test) can drive it.
    public class Conversation
    {
        private readonly DialogueDefinition _definition;
        private readonly List<DialogueChoice> _choices = new();

        public Conversation(DialogueDefinition definition, DialogueContext context)
        {
            _definition = definition;
            Context     = context;
        }

        public DialogueContext               Context { get; }
        public DialogueNode                  Current { get; private set; }
        public IReadOnlyList<DialogueChoice> Choices => _choices;
        public bool                          IsOver  { get; private set; }

        // The line or its choices changed.
        public event Action Changed;
        public event Action Ended;

        public void Start()
        {
            GoTo(_definition != null ? _definition.StartNode : null);
        }

        public void Choose(int index)
        {
            if (IsOver || index < 0 || index >= _choices.Count) return;

            DialogueChoice choice = _choices[index];

            // Move on first so an action that opens another screen (a shop) finds the
            // dialogue already closed.
            if (choice.EndsConversation) End();
            else GoTo(_definition.TryGetNode(choice.Next, out DialogueNode next) ? next : null);

            foreach (DialogueAction action in choice.Actions)
                if (action != null) action.Execute(Context);
        }

        public void End()
        {
            if (IsOver) return;

            IsOver = true;
            Current = null;
            _choices.Clear();
            Ended?.Invoke();
        }

        // A missing node (broken Next id, empty dialogue) ends the conversation rather than
        // leaving the player stuck.
        private void GoTo(DialogueNode node)
        {
            if (node == null)
            {
                End();
                return;
            }

            Current = node;
            _choices.Clear();
            foreach (DialogueChoice choice in node.Choices)
                if (choice != null && choice.IsAvailable(Context)) _choices.Add(choice);

            Changed?.Invoke();
        }
    }
}
