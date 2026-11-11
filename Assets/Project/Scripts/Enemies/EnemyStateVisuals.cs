using System.Collections;
using UnityEngine;

namespace CGD.Enemies
{
    // Tints the enemy per AI state (placeholder until animations/VFX exist): grey patrolling,
    // yellow investigating or heading home, orange hunting, red fighting, purple regrouping.
    // A tell (noticing something, committing to a fight) pulses it in the faction's colour,
    // in a rhythm of the faction's own: a sharp blink, a double beat, a long slow fade.
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyStateVisuals : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;

        [Header("State Colors")]
        [SerializeField] private Color _patrolColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _alertColor  = new Color(1f,   0.8f, 0f  );
        [SerializeField] private Color _huntColor   = new Color(1f,   0.5f, 0.1f);
        [SerializeField] private Color _chaseColor  = new Color(1f,   0.15f, 0.15f);
        [SerializeField] private Color _retreatColor = new Color(0.6f, 0.3f, 0.9f);
        [SerializeField] private Color _stunnedColor = new Color(0.3f, 0.6f, 1f);
        [SerializeField] private Color _deadColor    = new Color(0.15f, 0.15f, 0.15f);

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        private EnemyAI _ai;
        private MaterialPropertyBlock _mpb;
        private Color     _stateColor;
        private Coroutine _tell;

        private void Awake()
        {
            _ai  = GetComponent<EnemyAI>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _ai.StateChanged += Apply;
            _ai.Told         += OnTold;
            Apply(_ai.State);
        }

        private void OnDisable()
        {
            _ai.StateChanged -= Apply;
            _ai.Told         -= OnTold;
            _tell = null;
        }

        private void OnTold(TellKind kind)
        {
            if (_ai.State == EnemyAI.AiState.Dead) return;
            if (_tell != null) StopCoroutine(_tell);
            _tell = StartCoroutine(Pulse(kind));
        }

        private IEnumerator Pulse(TellKind kind)
        {
            (int pulses, float beat) = kind switch
            {
                TellKind.Scan   => (1, 0.25f),
                TellKind.LockOn => (2, 0.12f),
                TellKind.Sniff  => (2, 0.18f),
                TellKind.Roar   => (1, 0.6f),
                TellKind.Stare  => (1, 0.9f),
                _               => (1, 0.3f),
            };
            Color accent = _ai.Data.DamageNumberColor.a > 0f ? _ai.Data.DamageNumberColor : Color.white;

            for (int i = 0; i < pulses; i++)
            {
                Paint(accent);
                yield return new WaitForSeconds(beat);
                Paint(_stateColor);
                if (i < pulses - 1) yield return new WaitForSeconds(beat);
            }
            _tell = null;
        }

        private void Apply(EnemyAI.AiState state)
        {
            _stateColor = state switch
            {
                EnemyAI.AiState.Investigate => _alertColor,
                EnemyAI.AiState.Return      => _alertColor,
                EnemyAI.AiState.Hunt        => _huntColor,
                EnemyAI.AiState.Chase       => _chaseColor,
                EnemyAI.AiState.RaiseAlarm  => _chaseColor,
                EnemyAI.AiState.Retreat     => _retreatColor,
                EnemyAI.AiState.Stunned => _stunnedColor,
                EnemyAI.AiState.Dead    => _deadColor,
                _                       => _patrolColor,
            };

            // A pulse in progress carries on; it hands back to this colour when it ends.
            if (_tell == null) Paint(_stateColor);
        }

        private void Paint(Color color)
        {
            _mpb.SetColor(ColorId, color);
            foreach (var r in _renderers)
            {
                if (r != null) r.SetPropertyBlock(_mpb);
            }
        }
    }
}
