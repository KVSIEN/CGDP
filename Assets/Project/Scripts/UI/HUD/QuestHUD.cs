using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Quests;

namespace CGD.UI
{
    // Top-left list of active quests with their objectives (finished ones struck out) and a
    // countdown for timed quests. Rebuilt when a quest changes, and once a second while
    // a timer is running.
    [RequireComponent(typeof(RectTransform))]
    public class QuestHUD : HUDElement
    {
        [SerializeField] private QuestTracker _tracker;

        [Header("Layout")]
        [SerializeField] private Vector2 _screenPadding = new(20f, 120f);
        [SerializeField] private float   _width         = 320f;

        private static readonly Color PanelBg = new(0f, 0f, 0f, 0.45f);

        private readonly StringBuilder _text = new();
        private TextMeshProUGUI _label;
        private Image           _background;
        private int             _shownSeconds = -1;

        private void Awake()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(_screenPadding.x, -_screenPadding.y);
            rt.sizeDelta = new Vector2(_width, 0f);

            _background = UIFactory.MakeImage("Background", rt);
            _background.color = PanelBg;
            UIFactory.Stretch(_background.rectTransform);

            _label = UIFactory.MakeText("Quests", rt);
            _label.fontSize  = 14f;
            _label.alignment = TextAlignmentOptions.TopLeft;
            _label.richText  = true;
            UIFactory.Stretch(_label.rectTransform);
            _label.rectTransform.offsetMin = new Vector2(10f, 8f);
            _label.rectTransform.offsetMax = new Vector2(-10f, -8f);
        }

        private void OnEnable()
        {
            if (_tracker == null) return;

            _tracker.Log.QuestChanged += OnQuestChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.Log.QuestChanged -= OnQuestChanged;
        }

        private void Update()
        {
            int seconds = SoonestTimerSeconds();
            if (seconds != _shownSeconds) Refresh();
        }

        private void OnQuestChanged(QuestProgress quest) => Refresh();

        public override void Refresh()
        {
            if (_tracker == null || _label == null) return;

            _shownSeconds = SoonestTimerSeconds();
            _text.Clear();

            foreach (QuestProgress quest in _tracker.Log.Quests)
                if (quest.IsActive) AppendQuest(quest);

            _label.text = _text.ToString();
            _background.enabled = _text.Length > 0;

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(_width, _text.Length > 0 ? _label.preferredHeight + 16f : 0f);
        }

        private void AppendQuest(QuestProgress quest)
        {
            if (_text.Length > 0) _text.Append('\n');
            _text.Append("<b>").Append(quest.Definition.Title).Append("</b>");

            if (quest.Definition.HasTimeLimit)
                _text.Append("  <color=#FFB347>").Append(Mathf.CeilToInt(Mathf.Max(0f, quest.TimeRemaining))).Append("s</color>");

            foreach (ObjectiveProgress objective in quest.Objectives)
            {
                // Sequential quests only show objectives that are done or counting now.
                if (!objective.IsComplete && !quest.IsListening(objective)) continue;

                // Plain ASCII markers: the default TMP font has no check-mark glyph.
                _text.Append('\n').Append(objective.IsComplete ? "  <color=#8A8A8A><s>" : "  - ");
                _text.Append(objective.Definition.Description);
                if (objective.IsComplete) _text.Append("</s></color>");
                if (objective.Definition.IsOptional) _text.Append(" <i>(optional)</i>");
                if (objective.Definition.RequiredCount > 1)
                    _text.Append("  ").Append(objective.Count).Append('/').Append(objective.Definition.RequiredCount);
            }
        }

        // Whole seconds on the timed quest closest to failing, or -1 when none is running.
        private int SoonestTimerSeconds()
        {
            if (_tracker == null) return -1;

            // Indexed loop: runs every frame, and foreach over the interface would allocate.
            var quests = _tracker.Log.Quests;
            float soonest = float.MaxValue;
            for (int i = 0; i < quests.Count; i++)
                if (quests[i].IsActive && quests[i].Definition.HasTimeLimit)
                    soonest = Mathf.Min(soonest, quests[i].TimeRemaining);

            return soonest == float.MaxValue ? -1 : Mathf.CeilToInt(Mathf.Max(0f, soonest));
        }
    }
}
