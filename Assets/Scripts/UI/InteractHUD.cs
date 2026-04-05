using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Displays an interaction prompt ("[ E ]  Pick Up Rifle") when the player
// is within range of an IInteractable. The prompt child is toggled on/off
// independently of HUDElement.Show/Hide so HideAll() still works correctly.
[RequireComponent(typeof(RectTransform))]
public class InteractHUD : HUDElement
{
    [SerializeField] private PlayerInteraction _interaction;

    [Header("Layout")]
    [SerializeField] private float _screenPaddingBottom = 110f;
    [SerializeField] private float _panelWidth          = 340f;
    [SerializeField] private float _panelHeight         = 42f;

    private static readonly Color PanelBg   = new Color(0.04f, 0.04f, 0.06f, 0.88f);
    private static readonly Color KeyBg     = new Color(0.22f, 0.42f, 0.82f, 0.92f);
    private static readonly Color LabelColor = new Color(0.95f, 0.95f, 0.95f, 0.90f);
    private static readonly Color KeyColor   = new Color(1f,    1f,    1f,    1f);

    private GameObject         _prompt;
    private TextMeshProUGUI    _labelText;

    private void Awake()
    {
        BuildPrompt();
        _prompt.SetActive(false);
    }

    private void BuildPrompt()
    {
        var rt             = GetComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0f);
        rt.anchorMax       = new Vector2(0.5f, 0f);
        rt.pivot           = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, _screenPaddingBottom);
        rt.sizeDelta       = new Vector2(_panelWidth, _panelHeight);

        // Inner container — toggled by HasTarget state
        _prompt = new GameObject("Prompt", typeof(RectTransform));
        _prompt.transform.SetParent(transform, false);
        var promptRt = _prompt.GetComponent<RectTransform>();
        Stretch(promptRt);

        // Dark background
        var bg = MakeImage("Bg", promptRt);
        bg.color = PanelBg;
        Stretch(bg.rectTransform);

        float keySize = _panelHeight;

        // Blue key badge on the left
        var keyBg = MakeImage("KeyBg", promptRt);
        keyBg.color = KeyBg;
        keyBg.rectTransform.anchorMin = new Vector2(0f, 0f);
        keyBg.rectTransform.anchorMax = new Vector2(0f, 1f);
        keyBg.rectTransform.pivot     = new Vector2(0f, 0.5f);
        keyBg.rectTransform.anchoredPosition = Vector2.zero;
        keyBg.rectTransform.sizeDelta = new Vector2(keySize, 0f);

        var keyText = MakeText("KeyLabel", keyBg.rectTransform);
        keyText.text      = "E";
        keyText.fontSize  = 18f;
        keyText.fontStyle = FontStyles.Bold;
        keyText.color     = KeyColor;
        keyText.alignment = TextAlignmentOptions.Midline;
        Stretch(keyText.rectTransform);

        // Action label to the right of the key badge
        _labelText = MakeText("ActionLabel", promptRt);
        _labelText.fontSize  = 13f;
        _labelText.color     = LabelColor;
        _labelText.alignment = TextAlignmentOptions.MidlineLeft;
        _labelText.rectTransform.anchorMin       = new Vector2(0f, 0f);
        _labelText.rectTransform.anchorMax       = new Vector2(1f, 1f);
        _labelText.rectTransform.pivot           = new Vector2(0f, 0.5f);
        _labelText.rectTransform.anchoredPosition = new Vector2(keySize + 12f, 0f);
        _labelText.rectTransform.sizeDelta       = new Vector2(-(keySize + 20f), 0f);
    }

    private void Update()
    {
        if (_interaction == null) return;

        bool hasTarget = _interaction.HasTarget;

        if (_prompt.activeSelf != hasTarget)
            _prompt.SetActive(hasTarget);

        if (hasTarget)
            _labelText.text = _interaction.TargetLabel;
    }

    public override void Refresh() { }

    private static Image MakeImage(string n, RectTransform parent)
    {
        var go  = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI MakeText(string n, RectTransform parent)
    {
        var go   = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
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
