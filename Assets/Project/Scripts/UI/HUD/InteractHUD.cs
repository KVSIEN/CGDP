using UnityEngine;
using TMPro;
using CGD.Input;
using CGD.Interaction;
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


        private const float PromptWidth  = 340f;
        private const float PromptHeight = 42f;

        private Camera          _cam;
        private Canvas          _canvas;
        private GameObject      _worldPrompt;
        private TextMeshProUGUI _labelText;
        private TextMeshProUGUI _keyText;
        private RectTransform   _keyBadge;
        private RectTransform   _effortBar;

        // Each tap of a mash swells the key badge, which settles back.
        private const float PulseSize  = 0.3f;
        private const float PulseSpeed = 7f;
        private float _pulse;
        private int   _shownTaps;

        private void Awake()
        {
            BuildWorldPrompt();
            _worldPrompt.SetActive(false);
        }

        // Labels are built strings, so they're refreshed only when the target changes or
        // is used rather than every frame.
        private void OnEnable()
        {
            if (Bindings != null) Bindings.Changed += RefreshKey;
            RefreshKey();

            if (_interaction == null) return;

            _interaction.TargetChanged += RefreshLabel;
            RefreshLabel();
        }

        private void OnDisable()
        {
            if (Bindings != null) Bindings.Changed -= RefreshKey;
            if (_interaction != null) _interaction.TargetChanged -= RefreshLabel;
        }

        private void RefreshKey() => _keyText.text = KeyOf(GameAction.Interact);

        private void RefreshLabel()
        {
            string label = _interaction.TargetLabel;
            _labelText.text = _interaction.TargetStyle == InteractionStyle.Mash ? label + "  — mash" : label;
        }

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
            bg.color = Theme.Window;
            UIFactory.Stretch(bg.rectTransform);

            float keySize = PromptHeight;

            // Key badge
            var keyBg = UIFactory.MakeImage("KeyBg", rt, layer);
            _keyBadge = keyBg.rectTransform;
            keyBg.color = Theme.AccentStrong.WithAlpha(0.92f);
            keyBg.rectTransform.anchorMin        = new Vector2(0f, 0f);
            keyBg.rectTransform.anchorMax        = new Vector2(0f, 1f);
            keyBg.rectTransform.pivot            = new Vector2(0f, 0.5f);
            keyBg.rectTransform.anchoredPosition = Vector2.zero;
            keyBg.rectTransform.sizeDelta        = new Vector2(keySize, 0f);

            var keyText = UIFactory.MakeText("KeyLabel", keyBg.rectTransform, layer);
            _keyText          = keyText;
            keyText.fontSize  = 18f;
            keyText.fontStyle = FontStyles.Bold;
            keyText.color     = Color.white;
            keyText.alignment = TextAlignmentOptions.Midline;
            UIFactory.ShrinkToFit(keyText, 9f);
            UIFactory.Stretch(keyText.rectTransform);

            // Action label
            _labelText = UIFactory.MakeText("ActionLabel", rt, layer);
            _labelText.fontSize  = 13f;
            _labelText.color     = Theme.TextPrimary;
            _labelText.alignment = TextAlignmentOptions.MidlineLeft;
            _labelText.rectTransform.anchorMin        = new Vector2(0f, 0f);
            _labelText.rectTransform.anchorMax        = new Vector2(1f, 1f);
            _labelText.rectTransform.pivot            = new Vector2(0f, 0.5f);
            _labelText.rectTransform.anchoredPosition = new Vector2(keySize + 12f, 0f);
            _labelText.rectTransform.sizeDelta        = new Vector2(-(keySize + 20f), 0f);

            // Progress bar for holding or mashing
            var effort = UIFactory.MakeImage("EffortProgress", rt, layer);
            effort.color = Theme.Accent.WithAlpha(0.95f);
            _effortBar = effort.rectTransform;
            _effortBar.anchorMin = Vector2.zero;
            _effortBar.anchorMax = new Vector2(0f, 0.1f);
            _effortBar.offsetMin = _effortBar.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_interaction == null) return;

            bool hasTarget = _interaction.HasTarget;

            if (_worldPrompt.activeSelf != hasTarget)
                _worldPrompt.SetActive(hasTarget);

            if (!hasTarget) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            _effortBar.anchorMax = new Vector2(_interaction.EffortProgress, 0.1f);
            PulseOnTaps();

            Vector3 targetPos = _interaction.TargetPosition + Vector3.up * _yOffset;
            float   dist      = Vector3.Distance(_cam.transform.position, targetPos);
            float   unitPerPx = dist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height * UIScale.Factor;

            _worldPrompt.transform.position   = targetPos;
            _worldPrompt.transform.rotation   = _cam.transform.rotation;
            _worldPrompt.transform.localScale = Vector3.one * unitPerPx;
        }

        private void PulseOnTaps()
        {
            if (_interaction.TapCount != _shownTaps)
            {
                _shownTaps = _interaction.TapCount;
                _pulse = 1f;
            }

            _pulse = Mathf.MoveTowards(_pulse, 0f, PulseSpeed * Time.unscaledDeltaTime);
            _keyBadge.localScale = Vector3.one * (1f + PulseSize * _pulse);
        }

        public override void Hide()
        {
            base.Hide();
            if (_worldPrompt != null) _worldPrompt.SetActive(false);
        }

        public override void Refresh() { }
    }
}
