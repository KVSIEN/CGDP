using UnityEngine;
using TMPro;
using CGD.Stealth;

namespace CGD.UI
{
    // A small tag under the crosshair showing stealth: HIDDEN while concealed and unseen,
    // REVEALED right after making noise in cover, SPOTTED while an enemy can see you.
    // Nothing in the open when no one's looking.
    [RequireComponent(typeof(RectTransform))]
    public class StealthHUD : HUDElement
    {
        [SerializeField] private Stealthable _stealth;
        [SerializeField] private float _offsetBelowCentre = 60f;


        private TextMeshProUGUI _label;
        private string _shown;

        private void Awake()
        {
            var self = GetComponent<RectTransform>();
            self.anchorMin = self.anchorMax = self.pivot = new Vector2(0.5f, 0.5f);
            self.anchoredPosition = new Vector2(0f, -_offsetBelowCentre);
            self.sizeDelta = new Vector2(200f, 24f);

            _label = UIFactory.MakeText("Stealth", self);
            _label.fontSize  = 14f;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.Center;
            UIFactory.Stretch(_label.rectTransform);
            _label.text = string.Empty;
        }

        public override void Refresh() { }

        // Compares against the last shown state, so text is only set when it changes.
        private void Update()
        {
            if (_stealth == null) return;

            string text; Color color;
            if (_stealth.IsSpotted && _stealth.IsConcealed) { text = "SPOTTED";  color = Theme.Danger; }
            else if (_stealth.State == StealthState.Hidden)  { text = "HIDDEN";   color = Theme.Positive; }
            else if (_stealth.State == StealthState.Revealed){ text = "REVEALED"; color = Theme.Warning; }
            else                                             { text = string.Empty; color = Theme.Positive; }

            if (text == _shown) return;
            _shown = text;
            _label.text  = text;
            _label.color = color;
        }
    }
}
