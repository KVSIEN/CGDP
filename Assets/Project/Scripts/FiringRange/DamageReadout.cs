using UnityEngine;
using TMPro;
using CGD.Combat;
using CGD.UI;

namespace CGD.FiringRange
{
    // A small panel in the corner of the screen that reports what the player's hits do to the
    // range's dummies: the latest hit, the running fight's damage, hit count, damage per
    // second and, once the dummy drops, how long and how many hits it took. Only hits on
    // dummies count. It builds its own overlay canvas, so it stays out of the shared HUD prefab.
    public class DamageReadout : MonoBehaviour
    {
        private static readonly Color HeadColor = new(1f, 0.45f, 0.35f);

        [SerializeField] private Vector2 _size    = new(300f, 150f);
        [SerializeField] private Vector2 _padding = new(24f, 24f);

        private readonly DamageTally _tally = new();
        private TextMeshProUGUI _text;

        private void Awake() => BuildPanel();

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamageDealt;
            Show(null);
        }

        private void OnDisable() => CombatEvents.DamageDealt -= OnDamageDealt;

        private void OnDamageDealt(DamageReport report)
        {
            if (report.Source.Team != Team.Player || !report.Target.TryGetComponent<RangeDummy>(out _)) return;

            _tally.Record(Time.time, report.Amount, report.IsCritical, report.Killed);
            Show(report);
        }

        private void Show(DamageReport? last)
        {
            if (last is not { } report)
            {
                _text.text = "<b>DAMAGE</b>\nShoot a dummy";
                return;
            }

            float distance = report.Source.Owner != null
                ? Vector3.Distance(report.Source.Owner.transform.position, report.Point)
                : 0f;
            string hit = report.IsCritical
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(HeadColor)}>{report.Amount:0} HEAD</color>"
                : $"{report.Amount:0}";

            string text = $"<b>{report.Target.DisplayName}</b>  {distance:0} m\n" +
                          $"Last hit  {hit}\n" +
                          $"Hits  {_tally.Hits}  (head {_tally.Headshots})\n" +
                          $"Damage  {_tally.Damage:0}\n" +
                          $"DPS  {_tally.DamagePerSecond:0}";
            if (_tally.Killed)
                text += $"\n<b>Down in {_tally.Duration:0.00} s, {_tally.Hits} hits</b>";
            _text.text = text;
        }

        private void BuildPanel()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panel = UIFactory.MakeImage("Panel", (RectTransform)canvasObject.transform);
            panel.color = new Color(0f, 0f, 0f, 0.55f);
            UIFactory.AnchorToCorner(panel.rectTransform, new Vector2(0f, 0f), _padding);
            panel.rectTransform.sizeDelta = _size;

            _text = UIFactory.MakeText("Text", panel.rectTransform);
            UIFactory.Stretch(_text.rectTransform);
            _text.rectTransform.offsetMin = new Vector2(12f, 8f);
            _text.rectTransform.offsetMax = new Vector2(-12f, -8f);
            _text.fontSize  = 16f;
            _text.alignment = TextAlignmentOptions.TopLeft;
        }
    }
}
