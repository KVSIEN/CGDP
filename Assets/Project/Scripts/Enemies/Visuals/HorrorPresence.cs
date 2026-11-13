using UnityEngine;
using UnityEngine.AI;
using CGD.Audio;
using CGD.Core;

namespace CGD.Enemies
{
    // What makes a Void horror unsettling to look at:
    //   glitching — while its target looks straight at it and it stands still (frozen under the
    //               stare, or lying in wait), its body blinks out for a few frames now and then
    //               and twitches a hand's width when it comes back: it isn't quite there;
    //   peeking   — darting out of cover (the Peek node) it stutters into view, blinking several
    //               times in quick succession, with a sound;
    //   whispers  — near its target and not being looked at, it whispers now and then, so the
    //               target hears it before it sees it.
    // Only the body's own renderers blink (forceRenderingOff, so nothing else that shows or
    // hides them is disturbed), and only the model child twitches, never the agent itself.
    [RequireComponent(typeof(EnemyAI))]
    [DisallowMultipleComponent]
    public class HorrorPresence : MonoBehaviour
    {
        [Tooltip("The visual root that twitches when it glitches (a child of the agent)")]
        [SerializeField] private Transform _model;
        [Tooltip("The body renderers that blink out (not effect shells or trails)")]
        [SerializeField] private Renderer[] _body = System.Array.Empty<Renderer>();

        [Header("Glitch")]
        [Tooltip("Degrees either side of where its target faces that count as being looked at")]
        [SerializeField, Range(5f, 90f)] private float _watchedAngle = 35f;
        [Tooltip("Metres per second below which it counts as standing still")]
        [SerializeField, Min(0f)] private float _stillSpeed = 0.4f;
        [Tooltip("Seconds between blinks while it glitches (random within the range)")]
        [SerializeField] private FloatRange _glitchEvery = new(0.35f, 1.4f);
        [Tooltip("Seconds each blink lasts (random within the range)")]
        [SerializeField] private FloatRange _glitchFor = new(0.04f, 0.12f);
        [Tooltip("Metres the body jumps sideways when it comes back from a blink")]
        [SerializeField, Min(0f)] private float _twitch = 0.08f;
        [Tooltip("Blinks in quick succession as it darts out of cover")]
        [SerializeField, Min(0)] private int _peekBlinks = 3;

        [Header("Sound")]
        [Tooltip("Played as it darts out of cover. Empty = silent")]
        [SerializeField] private SoundBank _peekSound;
        [Tooltip("Played now and then while near its target and not being looked at. Empty = silent")]
        [SerializeField] private SoundBank _whisper;
        [Tooltip("Metres from its target within which it whispers")]
        [SerializeField, Min(0f)] private float _whisperRange = 12f;
        [Tooltip("Seconds between whispers (random within the range)")]
        [SerializeField] private FloatRange _whisperEvery = new(6f, 12f);

        private EnemyAI        _ai;
        private NavMeshAgent   _agent;
        private GlitchSchedule _glitch;
        private Vector3        _offset;
        private float          _nextWhisper;

        private void Awake()
        {
            _ai     = GetComponent<EnemyAI>();
            _agent  = GetComponent<NavMeshAgent>();
            _glitch = new GlitchSchedule(_glitchEvery, _glitchFor);
            _nextWhisper = _whisperEvery.EvaluateClamped();
        }

        private void OnEnable()
        {
            _ai.StepChanged  += OnStepChanged;
            _ai.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            _ai.StepChanged  -= OnStepChanged;
            _ai.StateChanged -= OnStateChanged;
            Restore();
        }

        private void Update()
        {
            if (_ai.State == EnemyAI.AiState.Dead) return;

            bool watched = Gaze.IsLookedAt(_ai, _watchedAngle);
            bool still   = _ai.Step == EngageStep.Freeze || _agent.velocity.sqrMagnitude < _stillSpeed * _stillSpeed;
            _glitch.Tick(Time.deltaTime, watched && still);
            ShowGlitch();

            TickWhisper(watched);
        }

        private void ShowGlitch()
        {
            if (_glitch.JustBlinked) Twitch();
            for (int i = 0; i < _body.Length; i++)
                if (_body[i] != null) _body[i].forceRenderingOff = _glitch.IsHidden;
        }

        // Comes back from each blink a little off from where it was.
        private void Twitch()
        {
            if (_model == null || _twitch <= 0f) return;

            Vector2 jump = Random.insideUnitCircle * _twitch;
            Vector3 next = new(jump.x, 0f, jump.y);
            _model.localPosition += next - _offset;
            _offset = next;
        }

        private void TickWhisper(bool watched)
        {
            _nextWhisper -= Time.deltaTime;
            if (_nextWhisper > 0f) return;
            _nextWhisper = _whisperEvery.EvaluateClamped();

            var target = _ai.Perception.Target;
            if (watched || target == null) return;
            if ((target.transform.position - transform.position).sqrMagnitude > _whisperRange * _whisperRange) return;
            _whisper.TryPlay(transform.position + Vector3.up * EnemyEyes.Height);
        }

        private void OnStepChanged(EngageStep step)
        {
            if (step != EngageStep.Peek) return;
            _glitch.Burst(_peekBlinks);
            _peekSound.TryPlay(transform.position + Vector3.up * EnemyEyes.Height);
        }

        // Dead (or respawned) it is just a body again: fully shown, back in its place.
        private void OnStateChanged(EnemyAI.AiState state)
        {
            if (state == EnemyAI.AiState.Dead || state == EnemyAI.AiState.Patrol) Restore();
        }

        private void Restore()
        {
            _glitch?.Reset();
            for (int i = 0; i < _body.Length; i++)
                if (_body[i] != null) _body[i].forceRenderingOff = false;
            if (_model != null) _model.localPosition -= _offset;
            _offset = Vector3.zero;
        }
    }
}
