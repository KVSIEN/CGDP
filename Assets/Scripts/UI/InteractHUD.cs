using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class InteractHUD : HUDElement
{
    [SerializeField] private PlayerInteraction _interaction;
    [SerializeField] private float _yOffset = 1.0f;  // world units above object

    private static readonly Color PanelBg    = new Color(0.04f, 0.04f, 0.06f, 0.88f);
    private static readonly Color KeyBg      = new Color(0.22f, 0.42f, 0.82f, 0.92f);
    private static readonly Color LabelColor = new Color(0.95f, 0.95f, 0.95f, 0.90f);
    private static readonly Color KeyColor   = new Color(1f, 1f, 1f, 1f);

    private const float PromptWidth  = 340f;
    private const float PromptHeight = 42f;

    private Camera          _cam;
    private Canvas          _canvas;
    private GameObject      _worldPrompt;
    private TextMeshProUGUI _labelText;

    private void Awake()
    {
        _cam = Camera.main;
        BuildWorldPrompt();
        _worldPrompt.SetActive(false);
    }

    private void BuildWorldPrompt()
    {
        int layer        = LayerMask.NameToLayer("UI");
        _worldPrompt     = new GameObject("InteractPrompt");
        _worldPrompt.layer = layer;

        _canvas                = _worldPrompt.AddComponent<Canvas>();
        _canvas.renderMode     = RenderMode.WorldSpace;
        _canvas.worldCamera    = DamagePopup.GetOrCreateOverlayCamera();
        _canvas.overrideSorting = true;
        _canvas.sortingOrder   = 1;

        var rt       = (RectTransform)_canvas.transform;
        rt.sizeDelta = new Vector2(PromptWidth, PromptHeight);

        // Background
        var bg = MakeImage("Bg", rt, layer);
        bg.color = PanelBg;
        Stretch(bg.rectTransform);

        float keySize = PromptHeight;

        // Key badge
        var keyBg = MakeImage("KeyBg", rt, layer);
        keyBg.color = KeyBg;
        keyBg.rectTransform.anchorMin        = new Vector2(0f, 0f);
        keyBg.rectTransform.anchorMax        = new Vector2(0f, 1f);
        keyBg.rectTransform.pivot            = new Vector2(0f, 0.5f);
        keyBg.rectTransform.anchoredPosition = Vector2.zero;
        keyBg.rectTransform.sizeDelta        = new Vector2(keySize, 0f);

        var keyText = MakeText("KeyLabel", keyBg.rectTransform, layer);
        keyText.text      = "E";
        keyText.fontSize  = 18f;
        keyText.fontStyle = FontStyles.Bold;
        keyText.color     = KeyColor;
        keyText.alignment = TextAlignmentOptions.Midline;
        Stretch(keyText.rectTransform);

        // Action label
        _labelText = MakeText("ActionLabel", rt, layer);
        _labelText.fontSize  = 13f;
        _labelText.color     = LabelColor;
        _labelText.alignment = TextAlignmentOptions.MidlineLeft;
        _labelText.rectTransform.anchorMin        = new Vector2(0f, 0f);
        _labelText.rectTransform.anchorMax        = new Vector2(1f, 1f);
        _labelText.rectTransform.pivot            = new Vector2(0f, 0.5f);
        _labelText.rectTransform.anchoredPosition = new Vector2(keySize + 12f, 0f);
        _labelText.rectTransform.sizeDelta        = new Vector2(-(keySize + 20f), 0f);
    }

    private void LateUpdate()
    {
        if (_interaction == null) return;

        bool hasTarget = _interaction.HasTarget;

        if (_worldPrompt.activeSelf != hasTarget)
            _worldPrompt.SetActive(hasTarget);

        if (!hasTarget) return;

        _labelText.text = _interaction.TargetLabel;

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

    private static Image MakeImage(string n, RectTransform parent, int layer)
    {
        var go   = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string n, RectTransform parent, int layer)
    {
        var go   = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = layer;
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
