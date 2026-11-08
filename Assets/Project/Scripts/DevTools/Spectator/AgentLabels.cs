using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CGD.Combat;
using CGD.Enemies;
using CGD.UI;

namespace CGD.DevTools
{
    // On-screen labels that follow enemies around: name with health (+ shield), what it is
    // doing (state · step) and what it believes (awareness → target). Text is rebuilt a few
    // times a second; positions follow every frame. A screen-space overlay of its own, so it
    // shows with the HUD switched off.
    internal sealed class AgentLabels
    {
        private const float RefreshEvery = 0.2f;
        private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

        private sealed class Label
        {
            public EnemyAI         Ai;
            public EnemyHealth     Health;
            public RectTransform   Panel;
            public TextMeshProUGUI Text;
        }

        private readonly Canvas  _canvas;
        private readonly Label[] _labels;
        private float _nextRefresh;

        public AgentLabels(Transform parent, EnemyAI[] agents, Vector2 size, float fontSize)
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

            _labels = new Label[agents.Length];
            for (int i = 0; i < agents.Length; i++)
                _labels[i] = Build(agents[i], (RectTransform)canvasObject.transform, size, fontSize);
        }

        public void SetVisible(bool visible) => _canvas.gameObject.SetActive(visible);

        public void Tick(Camera camera, float height, float time)
        {
            if (camera == null) return;
            bool refresh = time >= _nextRefresh;
            if (refresh) _nextRefresh = time + RefreshEvery;
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

            foreach (Label label in _labels)
            {
                if (label.Ai == null) continue;
                Vector3 screen = camera.WorldToScreenPoint(label.Ai.transform.position + Vector3.up * height);
                bool show = screen.z > 0f && label.Ai.gameObject.activeInHierarchy;
                if (label.Panel.gameObject.activeSelf != show) label.Panel.gameObject.SetActive(show);
                if (!show) continue;

                label.Panel.anchoredPosition = new Vector2(screen.x, screen.y) / scale;
                if (refresh) label.Text.text = Describe(label);
            }
        }

        private static string Describe(Label label)
        {
            EnemyAI     ai     = label.Ai;
            EnemyHealth health = label.Health;

            string shield = health.MaxShield > 0f ? $" <color=#7fc8ff>+{health.Shield:0}</color>" : "";
            string step   = ai.Step == EngageStep.None ? "" : $" · {ai.Step}";
            HealthManager target = ai.Perception.Target;
            string belief = target != null ? $"{ai.Awareness} → {target.DisplayName}" : ai.Awareness.ToString();
            return $"<b>{health.DisplayName}</b>  {health.Health:0}{shield}\n{ai.State}{step}\n<size=85%>{belief}</size>";
        }

        private static Label Build(EnemyAI ai, RectTransform canvas, Vector2 size, float fontSize)
        {
            Image panel = UIFactory.MakeImage($"Label {ai.name}", canvas);
            panel.color = new Color(0f, 0f, 0f, 0.55f);
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

            return new Label { Ai = ai, Health = ai.GetComponent<EnemyHealth>(), Panel = rect, Text = text };
        }
    }
}
