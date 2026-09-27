using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Player;

namespace CGD.Stealth
{
    // Lets a character (the Player) hide. It's concealed while standing in a
    // ConcealmentZone (bushes, smoke) or cloaked by a stealth ability; attacks and other
    // noise it makes reveal it for a moment. While Hidden, enemies only detect it inside
    // a short range — longer when they share the same cover — see StealthSettings.
    public class Stealthable : MonoBehaviour
    {
        // Noise further away than this (a grenade landing across the room) doesn't give the
        // thrower away; gunfire and swings at the character's position do.
        private const float OwnNoiseRadius = 4f;

        private static readonly Dictionary<HealthManager, Stealthable> _byHealth = new();

        [SerializeField] private StealthSettings _settings;
        [Tooltip("Optional — played the moment an enemy spots you while concealed")]
        [SerializeField] private FeedbackPreset _spottedFeedback;

        private readonly StealthStatus _status = new();
        private HealthManager  _health;
        private PlayerMovement _movement;

        public StealthState State       => _status.State;
        public bool         IsHidden    => _status.IsHidden;
        public bool         IsConcealed => _status.State != StealthState.Visible;
        public bool         IsCloaked   => _status.IsCloaked;
        public bool         IsSpotted   => _status.IsSpotted;
        public bool         InCover     { get; private set; }

        // (previous, next)
        public event Action<StealthState, StealthState> StateChanged
        {
            add    => _status.Changed += value;
            remove => _status.Changed -= value;
        }

        public event Action<bool> SpottedChanged
        {
            add    => _status.SpottedChanged += value;
            remove => _status.SpottedChanged -= value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _byHealth.Clear();

        // How perception finds the stealth of a character it's looking at, without a
        // GetComponent per check.
        public static bool TryGet(HealthManager character, out Stealthable stealth) =>
            _byHealth.TryGetValue(character, out stealth);

        private void Awake()
        {
            if (_settings == null) _settings = ScriptableObject.CreateInstance<StealthSettings>();
            TryGetComponent(out _health);
            TryGetComponent(out _movement);
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _byHealth[_health] = this;
                _health.OnRevived += _status.Reset;
            }
            Noise.Emitted += OnNoise;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _byHealth.Remove(_health);
                _health.OnRevived -= _status.Reset;
            }
            Noise.Emitted -= OnNoise;
            _status.Reset();
        }

        private void Update()
        {
            InCover = ConcealmentZone.AnyContains(transform.position, IsCrouching);
            _status.Tick(Time.deltaTime, InCover);
        }

        public void Cloak(float seconds, bool breaksOnAttack = true) => _status.Cloak(seconds, breaksOnAttack);

        public void EndCloak() => _status.EndCloak();

        // Whether an observer at this position can detect the character right now. While
        // not Hidden this is always true — normal sight and hearing rules then decide.
        public bool IsExposedTo(Vector3 observer)
        {
            if (!IsHidden) return true;

            Vector3 self = transform.position;
            bool sharedCover = ConcealmentZone.Share(observer, self);
            float range = _settings.RevealRange(sharedCover, IsCrouching, IsSprinting);
            return (observer - self).sqrMagnitude <= range * range;
        }

        // Perception calls this whenever an enemy detects the character.
        public void NotifySpotted()
        {
            bool wasSpotted = IsSpotted;
            _status.OnSpotted(_settings.SpottedMemory);
            if (!wasSpotted && IsConcealed) FeedbackBus.Play(_spottedFeedback);
        }

        // Noise this character makes where it stands (gunfire, swings, abilities) gives it
        // away. Silent weapons (noise radius 0) never emit, so they keep stealth.
        private void OnNoise(NoiseEvent noise)
        {
            GameObject owner = noise.Source.Owner;
            if (owner == null || !owner.transform.IsChildOf(transform)) return;
            if ((noise.Position - transform.position).sqrMagnitude > OwnNoiseRadius * OwnNoiseRadius) return;

            _status.OnNoise(_settings.RevealAfterNoise);
        }

        private bool IsCrouching => _movement != null && _movement.IsCrouching;
        private bool IsSprinting => _movement != null && _movement.IsSprinting;
    }
}
