using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.UI;

namespace CGD.DevTools
{
    // The scenario list (Scenarios, B): one button per AIScenario with its description underneath; a
    // click loads it and closes the list. Built on the spectator HUD's canvas, so it shares
    // its scaling and raycaster.
    internal sealed class ScenarioMenu
    {
        private const float Width     = 600f;
        private const float RowHeight = 62f;
        private const float Header    = 56f;
        private const float Footer    = 52f;

        private static readonly Color WindowColor = new(0.07f, 0.08f, 0.12f, 0.96f);
        private static readonly Color MutedText   = new(0.6f, 0.63f, 0.7f, 1f);

        private readonly GameObject _root;

        public ScenarioMenu(RectTransform canvas, AIScenario[] scenarios, string closeKey, string restartKey, Action<AIScenario> load)
        {
            int count = Mathf.Max(1, scenarios.Length);
            Image window = UIFactory.MakeImage("Scenarios", canvas);
            window.color = WindowColor;
            window.raycastTarget = true;
            RectTransform rect = window.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Width, Header + count * RowHeight + Footer);
            _root = window.gameObject;

            Label(rect, "Title", "Scenarios", 22f, new Vector2(20f, -14f), new Vector2(300f, 30f));
            TextMeshProUGUI hint = Label(rect, "Hint", $"click one to load it  ·  {restartKey} restarts it  ·  {closeKey} closes", 13f,
                                         new Vector2(Width - 340f, -18f), new Vector2(320f, 24f));
            hint.alignment = TextAlignmentOptions.MidlineRight;
            hint.color = MutedText;

            if (scenarios.Length == 0)
                Label(rect, "Empty", "No scenarios: add AIScenario assets to the Spectator Camera's Scenarios", 14f,
                      new Vector2(20f, -Header), new Vector2(Width - 40f, RowHeight)).color = MutedText;

            for (int i = 0; i < scenarios.Length; i++)
            {
                AIScenario scenario = scenarios[i];
                if (scenario == null) continue;
                float y = -Header - i * RowHeight;

                Button button = UIFactory.MakeButton("Load " + scenario.name, rect, scenario.DisplayName, out TextMeshProUGUI label);
                label.fontSize  = 16f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin    = new Vector4(12f, 0f, 0f, 0f);
                UIFactory.Place(button.GetComponent<RectTransform>(), new Vector2(20f, y), new Vector2(200f, 32f));
                button.onClick.AddListener(() => load(scenario));

                TextMeshProUGUI description = Label(rect, "About " + scenario.name, scenario.Description, 13f,
                                                    new Vector2(232f, y + 2f), new Vector2(Width - 252f, RowHeight - 6f));
                description.alignment = TextAlignmentOptions.TopLeft;
                description.color = MutedText;
            }
            _root.SetActive(false);
        }

        public bool IsOpen => _root.activeSelf;

        public void Toggle() => _root.SetActive(!IsOpen);

        public void Close() => _root.SetActive(false);

        private static TextMeshProUGUI Label(RectTransform parent, string name, string value, float size, Vector2 position, Vector2 extent)
        {
            TextMeshProUGUI text = UIFactory.MakeText(name, parent);
            text.text     = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            UIFactory.Place(text.rectTransform, position, extent);
            return text;
        }
    }
}
