using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Enemies;
using CGD.Feedback;

namespace CGD.Director
{
    // Gives the pacing a voice: as the director moves into a phase, the matching beats show a
    // line in the HUD feed and have an enemy near the player shout to the others — so the
    // player hears the peak coming and the calm settling in, from the enemies themselves.
    // Only enemies that already know something is up speak; a calm patrol stays quiet.
    public class DirectorNarration : MonoBehaviour
    {
        [SerializeField] private NarrationSettings _settings;
        [SerializeField] private GameDirector _director;
        [SerializeField] private Transform    _player;
        [Tooltip("Layers enemies' colliders are on")]
        [SerializeField] private LayerMask    _enemyMask = ~0;

        private readonly NearbyEnemies _nearby = new();
        private RandomStream _random;

        private void Awake()
        {
            if (_settings == null || _director == null || _player == null)
            {
                Debug.LogError($"{name}: DirectorNarration needs NarrationSettings, the GameDirector and the player.", this);
                enabled = false;
                return;
            }

            _random = Seed.Random().Stream();
        }

        private void OnEnable()
        {
            if (_director != null) _director.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_director != null) _director.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(PacingPhase previous, PacingPhase next)
        {
            foreach (NarrationBeat beat in _settings.Beats)
            {
                if (beat == null || beat.Phase != next || _random.Value > beat.Chance) continue;

                FeedbackBus.Notify(beat.Message, beat.Style);
                if (beat.EnemyBark != BarkKind.None) ShoutFromNearest(beat.EnemyBark);
            }
        }

        // The nearest enemy in earshot that isn't calm says it (if its voice and the level's
        // bark spacing allow).
        private void ShoutFromNearest(BarkKind bark)
        {
            Vector3 player = _player.position;
            IReadOnlyList<EnemyAI> nearby = _nearby.Around(player, _settings.BarkRadius, _enemyMask);

            EnemyAI speaker = null;
            float best = float.MaxValue;
            for (int i = 0; i < nearby.Count; i++)
            {
                EnemyAI enemy = nearby[i];
                if (enemy.Awareness == AwarenessLevel.Unaware) continue;

                float distance = (enemy.transform.position - player).sqrMagnitude;
                if (distance < best) { best = distance; speaker = enemy; }
            }

            if (speaker != null) speaker.Bark(bark);
        }
    }
}
