using UnityEngine;
using UnityEngine.Events;
using CGD.Feedback;
using CGD.Quests;

namespace CGD.Interaction
{
    // Keeps a Door shut until enough different conditions are met: terminals switched on
    // (LockTerminal), anything with a UnityEvent calling Satisfy (an EventInteractable, a
    // boss's death), or listed QuestSignals being raised. Put it on the door's object and
    // assign it to the Door's Condition. Required 0 = every listed signal (or 1 without any).
    public class ConditionLock : MonoBehaviour
    {
        [Tooltip("Shown on the door's prompt with the progress, e.g. 'Terminals 1/2'")]
        [SerializeField] private string _label = "Terminals";
        [Tooltip("Different conditions needed. 0 = all of the signals below (at least 1)")]
        [SerializeField, Min(0)] private int _required;
        [Tooltip("Each raised signal counts as one condition")]
        [SerializeField] private QuestSignal[] _signals = System.Array.Empty<QuestSignal>();
        [SerializeField] private UnityEvent _onOpened = new();

        public LockProgress Progress { get; private set; }

        public string Describe() => $"{_label} {Progress.Met}/{Progress.Required}";

        private void Awake()
        {
            Progress = new LockProgress(_required > 0 ? _required : _signals.Length);
            Progress.Opened += OnOpened;
        }

        private void OnEnable()  => QuestEvents.Reported += OnQuestEvent;
        private void OnDisable() => QuestEvents.Reported -= OnQuestEvent;

        // For a generated level, which knows how many terminals it placed.
        public void SetRequired(int required) => Progress.SetRequired(required);

        // Counts `source` as one met condition; for UnityEvents, pass the object doing it.
        public void Satisfy(Object source)
        {
            if (!Progress.Report(source) || Progress.IsOpen) return;
            FeedbackBus.Notify($"{_label} {Progress.Met}/{Progress.Required}");
        }

        private void OnQuestEvent(QuestEvent e)
        {
            if (e.Kind != ObjectiveKind.Signal || e.Target is not QuestSignal signal) return;
            if (System.Array.IndexOf(_signals, signal) >= 0) Satisfy(signal);
        }

        private void OnOpened()
        {
            FeedbackBus.Notify($"{_label} {Progress.Met}/{Progress.Required} — door unlocked");
            _onOpened.Invoke();
        }
    }
}
