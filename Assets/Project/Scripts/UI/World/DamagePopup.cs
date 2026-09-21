using UnityEngine;
using TMPro;

namespace CGD.UI
{
    // One floating damage number: white with a black outline, a deep orange when the hit was
    // critical. Owns its own look, motion and lifetime, while where it sits relative to the
    // other numbers is the business of DamageNumbers, which projects it every frame. Pooled and
    // built once, so a shotgun blast costs no allocations.
    public class DamagePopup : MonoBehaviour
    {
        private const float RectHeight = 60f; // canvas rect the rendered pixel height is scaled from
        private const float FontSize   = 40f; // within that rect, so glyphs fill FontSize/RectHeight of it

        private static readonly Color NormalColor = Color.white;
        private static readonly Color CritColor   = new Color(0.95f, 0.42f, 0.06f, 1f);

        private Canvas          _canvas;
        private CanvasGroup     _group;
        private TextMeshProUGUI _text;

        private Vector3 _anchor;    // impact point in world space
        private Vector2 _offset;    // where it sits now, in pixels from the anchor
        private Vector2 _velocity;  // px/s, thrown up out of the impact and pulled back down
        private float   _heightPx;
        private float   _age;
        private float   _hold;
        private float   _kick;
        private bool    _crit;

        public Vector3 Anchor => _anchor;
        public Vector2 Offset => _offset;
        public float   Age    => _age;

        public void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode      = RenderMode.WorldSpace;
            _canvas.overrideSorting = true;

            ((RectTransform)_canvas.transform).sizeDelta = new Vector2(200f, RectHeight);

            _group = gameObject.AddComponent<CanvasGroup>();

            _text = UIFactory.MakeText("Text", (RectTransform)transform, gameObject.layer);
            UIFactory.Stretch(_text.rectTransform);
            _text.alignment          = TextAlignmentOptions.Center;
            _text.textWrappingMode   = TextWrappingModes.NoWrap;
            _text.outlineWidth       = 0.3f;
            _text.outlineColor       = Color.black;
        }

        // stackDepth is how many numbers were already clustered here, which decides the slot
        // this one takes in the group and which way it is thrown.
        public void Show(float damage, Vector3 anchor, bool crit, int stackDepth, int sortingOrder)
        {
            int   rounded = Mathf.Max(1, Mathf.RoundToInt(damage));
            float weight  = Mathf.Clamp01(damage / DamageNumberStyle.SizeRefDamage);

            _crit     = crit;
            _anchor   = anchor;
            _age      = 0f;
            _kick     = 0f;
            _hold     = DamageNumberStyle.HoldSeconds + (crit ? DamageNumberStyle.CritHoldBonus : 0f);
            _heightPx = Mathf.Lerp(DamageNumberStyle.MinHeightPx, DamageNumberStyle.MaxHeightPx, weight)
                      * (crit ? DamageNumberStyle.CritSizeScale : 1f);

            _offset = DamageNumberLayout.SlotOffset(stackDepth);
            // Thrown up and away from the middle of the group, so a group opens slightly as it
            // rises instead of every number tracking the same line up the screen.
            float side = _offset.x > 0.01f ? 1f : _offset.x < -0.01f ? -1f : 0f;
            _velocity  = new Vector2(side * DamageNumberStyle.SideSpeedPx, DamageNumberStyle.RiseSpeedPx);

            _text.text      = rounded.ToString();
            _text.fontSize  = FontSize;
            _text.fontStyle = crit ? FontStyles.Bold : FontStyles.Normal;
            _text.color     = crit ? CritColor : NormalColor;

            // Re-fetched on every show: a pooled number can outlive the camera it last rendered
            // through, and a scene load builds a new one.
            _canvas.worldCamera  = UIOverlayCamera.GetOrCreate();
            // Left disabled until it has been placed, so a reused number can never flash for a
            // frame at the position its last life ended in.
            _canvas.enabled      = false;
            _canvas.sortingOrder = sortingOrder;
            _group.alpha         = 1f;
            gameObject.SetActive(true);
        }

        // Bumped when another hit lands in the same cluster.
        public void Kick() => _kick = DamageNumberStyle.KickSeconds;

        // Starts this number fading now, however much hold it had left.
        public void Retire() => _hold = Mathf.Min(_hold, _age);

        // Advances motion and fade. Returns false once the number is spent.
        public bool Tick(float deltaTime)
        {
            _age += deltaTime;

            _velocity.y -= DamageNumberStyle.GravityPx * deltaTime;
            _offset     += _velocity * deltaTime;

            if (_kick > 0f) _kick -= deltaTime;

            float fadeAge = _age - _hold;
            if (fadeAge <= 0f)
            {
                _group.alpha = 1f;
                return true;
            }

            _group.alpha = 1f - fadeAge / DamageNumberStyle.FadeSeconds;
            return _group.alpha > 0f;
        }

        // Places the number at the screen position worked out for it, at the anchor's depth,
        // sized so its rendered height matches _heightPx however far away it is.
        public void Place(Camera camera, Vector2 screenPosition, float depth, float pixelHeightToWorld)
        {
            var rt = (RectTransform)transform;
            rt.position   = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            rt.rotation   = camera.transform.rotation;
            rt.localScale = Vector3.one * (_heightPx * pixelHeightToWorld / RectHeight * Scale());
        }

        public void SetVisible(bool visible)
        {
            if (_canvas.enabled != visible) _canvas.enabled = visible;
        }

        // Pops in past full size and settles back, then rides any kicks from later hits.
        private float Scale()
        {
            float scale = 1f;

            float peak = DamageNumberStyle.PunchPeakScale + (_crit ? DamageNumberStyle.CritPunchBonus : 0f);
            if (_age < DamageNumberStyle.PunchRise)
                scale = Mathf.Lerp(DamageNumberStyle.PunchFromScale, peak, _age / DamageNumberStyle.PunchRise);
            else if (_age < DamageNumberStyle.PunchRise + DamageNumberStyle.PunchSettle)
                scale = Mathf.Lerp(peak, 1f, (_age - DamageNumberStyle.PunchRise) / DamageNumberStyle.PunchSettle);

            if (_kick > 0f)
                scale += DamageNumberStyle.KickScale * (_kick / DamageNumberStyle.KickSeconds);

            return scale;
        }
    }
}
