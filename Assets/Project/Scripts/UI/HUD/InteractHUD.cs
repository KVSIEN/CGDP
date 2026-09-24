using UnityEngine;
using TMPro;
using CGD.Player;

namespace CGD.UI
{
    // World-space prompt above the current interactable; hold interactions show a
    // progress bar along the bottom edge.
    [RequireComponent(typeof(RectTransform))]
    public class InteractHUD : HUDElement
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private float _yOffset = 1.0f;  // world units above object

        private static readonly Color PanelBg    = new Color(0.04f, 0.04f, 0.06f, 0.88f);
        private static readonly Color KeyBg      = new Color(0.22f, 0.42f, 0.82f, 0.92f);
        private static readonly Color LabelColor = new Color(0.95f, 0.95f, 0.95f, 0.90f);
        private static readonly Color KeyColor   = new Color(1f, 1f, 1f, 1f);
        private static readonly Color HoldColor  = new Color(0.35f, 0.65f, 1f, 0.95f);

        private const float PromptWidth  = 340f;
        private const float PromptHeight = 42f;

        private Camera          _cam;
        private Canvas          _canvas;
        private GameObject      _worldPrompt;
        private TextMeshProUGUI _labelText;
        private RectTransform   _holdBar;

        private void Awake()
        {
            _cam = Camera.main;
            BuildWorldPrompt();
            _worldPrompt.SetActive(false);
        }

        // Labels are built strings, so they're refreshed only when the target changes or
        // is used rather than every frame.
        private void OnEnable()
        {
            if (_interaction == null) return;

            _interaction.TargetChanged += RefreshLabel;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (_interaction != null) _interaction.TargetChanged -= RefreshLabel;
        }

        private void RefreshLabel() => _labelText.text = _interaction.TargetLabel;

        private void BuildWorldPrompt()
        {
            int layer        = LayerMask.NameToLayer("UI");
            _worldPrompt     = new GameObject("InteractPrompt");
            _worldPrompt.layer = layer;

            _canvas                = _worldPrompt.AddComponent<Canvas>();
            _canvas.renderMode     = RenderMode.WorldSpace;
            _canvas.worldCamera    = UIOverlayCamera.GetOrCreate();
            _canvas.overrideSorting = true;
            _canvas.sortingOrder   = 1;

            var rt       = (RectTransform)_canvas.transform;
            rt.sizeDelta = new Vector2(PromptWidth, PromptHeight);

            // Background
            var bg = UIFactory.MakeImage("Bg", rt, layer);
            bg.color = PanelBg;
            UIFactory.Stretch(bg.rectTransform);

            float keySize = PromptHeight;

            // Key badge
            var keyBg = UIFactory.MakeImage("KeyBg", rt, layer);
            keyBg.color = KeyBg;
            keyBg.rectTransform.anchorMin        = new Vector2(0f, 0f);
            keyBg.rectTransform.anchorMax        = new Vector2(0f, 1f);
            keyBg.rectTransform.pivot            = new Vector2(0f, 0.5f);
            keyBg.rectTransform.anchoredPosition = Vector2.zero;
            keyBg.rectTransform.sizeDelta        = new Vector2(keySize, 0f);

            var keyText = UIFactory.MakeText("KeyLabel", keyBg.rectTransform, layer);
            keyText.text      = "E";
            keyText.fontSize  = 18f;
            keyText.fontStyle = FontStyles.Bold;
            keyText.color     = KeyColor;
            keyText.alignment = TextAlignmentOptions.Midline;
            UIFactory.Stretch(keyText.rectTransform);

            // Action label
            _labelText = UIFactory.MakeText("ActionLabel", rt, layer);
            _labelText.fontSize  = 13f;
            _labelText.color     = LabelColor;
            _labelText.alignment = TextAlignmentOptions.MidlineLeft;
            _labelText.rectTransform.anchorMin        = new Vector2(0f, 0f);
            _labelText.rectTransform.anchorMax        = new Vector2(1f, 1f);
            _labelText.rectTransform.pivot            = new Vector2(0f, 0.5f);
            _labelText.rectTransform.anchoredPosition = new Vector2(keySize + 12f, 0f);
            _labelText.rectTransform.sizeDelta        = new Vector2(-(keySize + 20f), 0f);

            // Hold progress bar
            var hold = UIFactory.MakeImage("HoldProgress", rt, layer);
            hold.color = HoldColor;
            _holdBar = hold.rectTransform;
            _holdBar.anchorMin = Vector2.zero;
            _holdBar.anchorMax = new Vector2(0f, 0.1f);
            _holdBar.offsetMin = _holdBar.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_interaction == null) return;

            bool hasTarget = _interaction.HasTarget;

            if (_worldPrompt.activeSelf != hasTarget)
                _worldPrompt.SetActive(hasTarget);

            if (!hasTarget) return;

            _holdBar.anchorMax = new Vector2(_interaction.HoldProgress, 0.1f);

            Vector3 targetPos = _interaction.TargetPosition + Vector3.up * _yOffset;
            float   dist      = Vector3.Distance(_cam.transform.position, targetPos);
            float   unitPerPx = dist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height;

            _worldPrompt.transform.position   = targetPos;
            _worldPrompt.transform.rotation   = _cam.transform.rotation;
            _worldPrompt.transform.localScale = Vector3.one * unitPerPx;
        }

        public override void Hide()
        {
            base.Hide();
            if (_worldPrompt != null) _worldPrompt.SetActive(false);
        }

        public override void Refresh() { }
    }
}
