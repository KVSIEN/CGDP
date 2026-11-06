using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Interaction;

namespace CGD.Enemies
{
    // A wall panel enemies can run to and raise the alarm with: every ally within its radius
    // hears it (walls don't stop a siren), goes looking where the caller last saw the target
    // and stays on edge. Enemies only use panels near their own post — ones they'd know about.
    // The player can stop it: kill the runner first, or disable the panel (hold Interact).
    public class AlarmPanel : MonoBehaviour, IInteractable
    {
        private static readonly List<AlarmPanel> Panels = new();
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Metres: allies within this range hear the alarm")]
        [SerializeField, Min(1f)] private float _radius = 60f;
        [Tooltip("Seconds an enemy spends at the panel raising the alarm")]
        [SerializeField, Min(0f)] private float _useTime = 1.5f;
        [Tooltip("Seconds before it can be raised again")]
        [SerializeField, Min(0f)] private float _rearmTime = 30f;
        [Tooltip("Seconds the player must hold Interact to disable it for good")]
        [SerializeField, Min(0f)] private float _disableTime = 1.5f;
        [SerializeField] private Team _team = Team.Enemy;

        [Header("Look")]
        [Tooltip("Optional — tinted to show armed / sounding / disabled")]
        [SerializeField] private Renderer _screen;
        [SerializeField] private Color _armedColor    = new(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color _soundingColor = new(1f, 0.15f, 0.1f);
        [SerializeField] private Color _disabledColor = new(0.15f, 0.15f, 0.15f);

        private MaterialPropertyBlock _block;
        private float _readyAt;
        private bool  _disabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Panels.Clear();

        public static IReadOnlyList<AlarmPanel> All => Panels;

        public bool  IsAvailable => !_disabled && Time.time >= _readyAt;
        public float UseTime     => _useTime;
        public Team  Team        => _team;

        private void OnEnable()
        {
            Panels.Add(this);
            Tint();
        }

        private void OnDisable() => Panels.Remove(this);

        private void Update()
        {
            // Back to armed once the siren is over.
            if (!_disabled && _readyAt > 0f && Time.time >= _readyAt)
            {
                _readyAt = 0f;
                Tint();
            }
        }

        // Sounds the alarm for whoever raised it, reporting where it last saw the target.
        public void Raise(EnemyAI by, Vector3 reported)
        {
            if (!IsAvailable) return;

            _readyAt = Time.time + _rearmTime;
            Tint();
            EnemyCallouts.Raise(new EnemyCallout(by, transform.position, reported, _team, _radius, throughWalls: true));
        }

        // --- IInteractable ------------------------------------------------------------

        public string GetInteractLabel(GameObject interactor) => "Disable alarm";

        public bool CanInteract(GameObject interactor) => !_disabled;

        public float HoldDuration => _disableTime;

        public void Interact(GameObject interactor)
        {
            _disabled = true;
            Tint();
        }

        private void Tint()
        {
            if (_screen == null) return;

            Color color = _disabled ? _disabledColor : Time.time < _readyAt ? _soundingColor : _armedColor;
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(ColorId, color);
            _screen.SetPropertyBlock(_block);
        }
    }
}
