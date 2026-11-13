using UnityEngine;

namespace CGD.Enemies
{
    // The marksman's warning: while it aims (the Aim step, from Aimed Shot) a thin line runs from
    // its eye to where it believes the target is, brightening as the shot comes. Built at
    // runtime from one material; hidden the rest of the time, so it costs nothing.
    [RequireComponent(typeof(EnemyAI))]
    [DisallowMultipleComponent]
    public class AimLaser : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");
        private const float ChestHeight = 1.1f;

        [Tooltip("Material for the line (an unlit or additive one reads best)")]
        [SerializeField] private Material _material;
        [SerializeField] private Color _color = new(1f, 0.15f, 0.1f, 0.85f);
        [Tooltip("Metres: width of the line")]
        [SerializeField, Min(0.001f)] private float _width = 0.025f;
        [Tooltip("Opacity at the start of the aim; it rises to full by the shot")]
        [SerializeField, Range(0f, 1f)] private float _startOpacity = 0.3f;
        [Tooltip("Seconds the aim usually lasts, to time the brightening (match the Aimed Shot node)")]
        [SerializeField, Min(0.05f)] private float _aimTime = 1f;

        private EnemyAI _ai;
        private LineRenderer _line;
        private MaterialPropertyBlock _block;
        private float _aimingFor;

        private void Awake()
        {
            _ai = GetComponent<EnemyAI>();
            _block = new MaterialPropertyBlock();

            var holder = new GameObject("AimLaser");
            holder.transform.SetParent(transform, false);
            _line = holder.AddComponent<LineRenderer>();
            _line.sharedMaterial    = _material;
            _line.positionCount     = 2;
            _line.useWorldSpace     = true;
            _line.startWidth        = _width;
            _line.endWidth          = _width;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows    = false;
            _line.enabled           = false;
        }

        private void LateUpdate()
        {
            bool aiming = _ai.State != EnemyAI.AiState.Dead && _ai.Step == EngageStep.Aim && _ai.Perception.Target != null;
            _line.enabled = aiming;
            if (!aiming)
            {
                _aimingFor = 0f;
                return;
            }

            _aimingFor += Time.deltaTime;
            _line.SetPosition(0, transform.position + Vector3.up * EnemyEyes.Height);
            _line.SetPosition(1, _ai.Perception.TargetPosition + Vector3.up * ChestHeight);

            Color color = _color;
            color.a *= Mathf.Lerp(_startOpacity, 1f, Mathf.Clamp01(_aimingFor / _aimTime));
            _line.startColor = color;
            _line.endColor   = color;
            _block.SetColor(ColorId, color);
            _line.SetPropertyBlock(_block);
        }

        private void OnDisable()
        {
            if (_line != null) _line.enabled = false;
        }
    }
}
