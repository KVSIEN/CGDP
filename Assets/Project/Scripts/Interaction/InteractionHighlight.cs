using UnityEngine;

namespace CGD.Interaction
{
    // Tints an interactable's renderers while the player is aiming at it, so it's clear
    // which of several nearby objects the Interact key will use. Add next to the
    // IInteractable (on its collider's GameObject).
    public class InteractionHighlight : MonoBehaviour, IInteractionFocusListener
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Empty = every renderer on this object and its children")]
        [SerializeField] private Renderer[] _renderers;
        [SerializeField] private Color _highlightColor = new Color(1f, 0.85f, 0.35f, 1f);
        [SerializeField, Range(0f, 1f)] private float _strength = 0.45f;

        private MaterialPropertyBlock _block;
        private Color[] _baseColors;

        private void Awake()
        {
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>();

            _block      = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material material = _renderers[i] != null ? _renderers[i].sharedMaterial : null;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            }
        }

        private void OnDisable() => OnInteractionFocus(false);

        public void OnInteractionFocus(bool focused)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;

                if (!focused)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }

                _block.SetColor(BaseColorId, Color.Lerp(_baseColors[i], _highlightColor, _strength));
                r.SetPropertyBlock(_block);
            }
        }
    }
}
