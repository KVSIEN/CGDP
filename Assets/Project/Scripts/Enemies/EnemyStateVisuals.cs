using UnityEngine;

namespace CGD.Enemies
{
    // Tints the enemy per AI state (placeholder until animations/VFX exist).
    [RequireComponent(typeof(EnemyAI))]
    public class EnemyStateVisuals : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;

        [Header("State Colors")]
        [SerializeField] private Color _patrolColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _alertColor  = new Color(1f,   0.8f, 0f  );
        [SerializeField] private Color _chaseColor  = new Color(1f,   0.15f, 0.15f);

        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        private EnemyAI _ai;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _ai  = GetComponent<EnemyAI>();
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _ai.StateChanged += Apply;
            Apply(_ai.State);
        }

        private void OnDisable() => _ai.StateChanged -= Apply;

        private void Apply(EnemyAI.AiState state)
        {
            Color color = state switch
            {
                EnemyAI.AiState.Alert => _alertColor,
                EnemyAI.AiState.Chase => _chaseColor,
                _                     => _patrolColor,
            };

            _mpb.SetColor(ColorId, color);
            foreach (var r in _renderers)
            {
                if (r != null) r.SetPropertyBlock(_mpb);
            }
        }
    }
}
