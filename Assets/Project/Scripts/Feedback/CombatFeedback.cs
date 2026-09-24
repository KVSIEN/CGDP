using UnityEngine;
using CGD.Combat;
using CGD.UI;

namespace CGD.Feedback
{
    // Combat responses for the character it sits on (the Player): hit markers and
    // presets for hits, criticals and kills it deals, a preset scaled by how hard it was
    // hit, and a one-off warning when health drops below a threshold.
    public class CombatFeedback : MonoBehaviour
    {
        [SerializeField] private HitMarkerHUD _hitMarker;

        [Header("Dealing damage")]
        [SerializeField] private FeedbackPreset _hit;
        [SerializeField] private FeedbackPreset _criticalHit;
        [Tooltip("{0} = the target's name")]
        [SerializeField] private FeedbackPreset _kill;

        [Header("Taking damage")]
        [Tooltip("Intensity scales with the hit as a share of max health")]
        [SerializeField] private FeedbackPreset _damageTaken;
        [SerializeField] private FeedbackPreset _lowHealth;
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;

        private HealthManager _health;
        private bool _lowHealthWarned;

        private void Awake() => TryGetComponent(out _health);

        private void OnEnable()
        {
            CombatEvents.DamageDealt += OnDamageDealt;
            if (_health == null) return;

            _health.OnDamaged += OnDamaged;
            _health.OnChanged += CheckLowHealth;
        }

        private void OnDisable()
        {
            CombatEvents.DamageDealt -= OnDamageDealt;
            if (_health == null) return;

            _health.OnDamaged -= OnDamaged;
            _health.OnChanged -= CheckLowHealth;
        }

        private void OnDamageDealt(DamageReport report)
        {
            if (!IsMine(report.Source) || report.Target == _health) return;

            if (report.Killed)
            {
                ShowMarker(HitMarkerHUD.Kind.Kill);
                FeedbackBus.Play(_kill, report.Target.DisplayName);
            }
            else if (report.IsCritical)
            {
                ShowMarker(HitMarkerHUD.Kind.Critical);
                FeedbackBus.Play(_criticalHit);
            }
            else
            {
                ShowMarker(HitMarkerHUD.Kind.Hit);
                FeedbackBus.Play(_hit);
            }
        }

        private void OnDamaged(float amount)
        {
            float share = _health.MaxHealth > 0f ? amount / _health.MaxHealth : 1f;
            FeedbackBus.Play(_damageTaken, intensity: Mathf.Clamp(share * 4f, 0.25f, 1f));
        }

        // Warns once on the way down; re-arms once health is back above the threshold.
        private void CheckLowHealth()
        {
            if (_health.MaxHealth <= 0f) return;

            bool low = !_health.IsDead && _health.Health / _health.MaxHealth <= _lowHealthThreshold;
            if (low && !_lowHealthWarned) FeedbackBus.Play(_lowHealth);
            _lowHealthWarned = low;
        }

        // Weapons, grenades and abilities all live on or under the character.
        private bool IsMine(DamageSource source) =>
            source.Owner != null && source.Owner.transform.IsChildOf(transform);

        private void ShowMarker(HitMarkerHUD.Kind kind)
        {
            if (_hitMarker != null) _hitMarker.Show(kind);
        }
    }
}
