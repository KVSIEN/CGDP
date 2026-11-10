using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Combat;
using CGD.Enemies;
using CGD.Stealth;
using CGD.UI;

namespace CGD.DevTools
{
    // On-screen labels that follow enemies around: its number (the key that watches it),
    // name and health (+ shield), what it is doing (state · step, and whether it is hidden in a
    // bush, cloaked or giving itself away) and what it believes (awareness → target). The inspected agent's label is highlighted. Text is rebuilt a few
    // times a second; positions follow every frame. A screen-space overlay of its own, so it
    // shows with the game HUD switched off.
    internal sealed class AgentLabels
    {
        private const float RefreshEvery = 0.2f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
        private static readonly Color Normal      = new(0f, 0f, 0f, 0.55f);
        private static readonly Color Highlighted = new(0.32f, 0.24f, 0.02f, 0.88f);

        private sealed class Label
        {
            public EnemyAI         Ai;
            public EnemyHealth     Health;
            public Stealthable     Stealth;
            public RectTransform   Panel;
            public Image           Background;
            public TextMeshProUGUI Text;
        }

        private readonly Canvas  _canvas;
        private readonly List<Label> _labels = new();
        private readonly RectTransform _labelParent;
        private readonly Vector2 _labelSize;
        private readonly float   _labelFontSize;
        private float _labelScale = 1f;
        private float _nextRefresh;
        private int   _highlighted = -1;

        public AgentLabels(Transform parent, AgentRoster agents, Vector2 size, float fontSize)
        {
            var canvasObject = new GameObject("Agent Labels", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight  = 0.5f;

            _labelParent   = (RectTransform)canvasObject.transform;
            _labelSize     = size;
            _labelFontSize = fontSize;
            for (int i = 0; i < agents.Count; i++)
                _labels.Add(Build(agents[i], _labelParent, size, fontSize));
            agents.Added += OnAdded;
        }

        private void OnAdded(EnemyAI agent)
        {
            Label label = Build(agent, _labelParent, _labelSize, _labelFontSize);
            label.Panel.localScale = Vector3.one * _labelScale;
            _labels.Add(label);
        }

        public void SetVisible(bool visible) => _canvas.gameObject.SetActive(visible);

        public void SetScale(float scale)
        {
            _labelScale = scale;
            foreach (Label label in _labels) label.Panel.localScale = Vector3.one * scale;
        }

        // −1 = none.
        public void Highlight(int index)
        {
            if (index == _highlighted) return;
            _highlighted = index;
            for (int i = 0; i < _labels.Count; i++)
                _labels[i].Background.color = i == index ? Highlighted : Normal;
            _nextRefresh = 0f;
        }

        public void Tick(Camera camera, float height, float time)
        {
            if (camera == null || !_canvas.gameObject.activeSelf) return;
            bool refresh = time >= _nextRefresh;
            if (refresh) _nextRefresh = time + RefreshEvery;
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

            for (int i = 0; i < _labels.Count; i++)
            {
                Label label = _labels[i];
                if (label.Ai == null) continue;
                Vector3 screen = camera.WorldToScreenPoint(label.Ai.transform.position + Vector3.up * height);
                bool show = screen.z > 0f && label.Ai.gameObject.activeInHierarchy;
                if (label.Panel.gameObject.activeSelf != show) label.Panel.gameObject.SetActive(show);
                if (!show) continue;

                label.Panel.anchoredPosition = new Vector2(screen.x, screen.y) / scale;
                if (refresh) label.Text.text = Describe(label, i);
            }
        }

        private static string Describe(Label label, int index)
        {
            EnemyAI     ai     = label.Ai;
            EnemyHealth health = label.Health;

            string number = index < 9 ? $"<color=#FFD24A>{index + 1}</color>  " : "";
            string shield = health.MaxShield > 0f ? $" <color=#7fc8ff>+{health.Shield:0}</color>" : "";
            string step   = ai.Step == EngageStep.None ? "" : $" · {ai.Step}";
            HealthManager target = ai.Perception.Target;
            string belief = target != null ? $"{ai.Awareness} → {target.DisplayName}" : ai.Awareness.ToString();
            return $"{number}<b>{health.DisplayName}</b>  {health.Health:0}{shield}\n{ai.State}{step}{StealthTag(label.Stealth)}\n<size=85%>{belief}</size>";
        }

        // Concealed agents fade to a ghost in the Game view; the label says why, and whether it is
        // a bush (hidden), its own cloak, or a moment of giving itself away (revealed).
        private static string StealthTag(Stealthable stealth)
        {
            if (stealth == null || !stealth.IsConcealed) return "";
            if (stealth.State == StealthState.Revealed) return "  <color=#FFB347>revealed</color>";
            return stealth.IsCloaked ? "  <color=#7CFF8A>cloaked</color>" : "  <color=#7CFF8A>hidden</color>";
        }

        private static Label Build(EnemyAI ai, RectTransform canvas, Vector2 size, float fontSize)
        {
            Image panel = UIFactory.MakeImage($"Label {ai.name}", canvas);
            panel.color = Normal;
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;   // positioned in screen pixels from the bottom left
            rect.pivot     = new Vector2(0.5f, 0f);           // sits above the point it marks
            rect.sizeDelta = size;

            TextMeshProUGUI text = UIFactory.MakeText("Text", rect);
            UIFactory.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            text.fontSize  = fontSize;
            text.alignment = TextAlignmentOptions.Center;

            ai.TryGetComponent(out Stealthable stealth);
            return new Label { Ai = ai, Health = ai.GetComponent<EnemyHealth>(), Stealth = stealth, Panel = rect, Background = panel, Text = text };
        }
    }
}
