using UnityEngine;

namespace CGD.Interaction
{
    // A console that counts toward a ConditionLock once switched on (hold Interact).
    // Hand-placed terminals reference their lock; a generated level binds them with Bind.
    [RequireComponent(typeof(Collider))]
    public class LockTerminal : MonoBehaviour, IInteractable
    {
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private ConditionLock _lock;
        [SerializeField] private string _label = "Restore power";
        [SerializeField, Min(0f)] private float _holdDuration = 1.5f;
        [Tooltip("Optional light that turns from Off to On colour when switched on")]
        [SerializeField] private Renderer _indicator;
        [SerializeField] private Color _offColor = new(1f, 0.25f, 0.2f);
        [SerializeField] private Color _onColor  = new(0.3f, 1f, 0.4f);

        private bool _isOn;
        private MaterialPropertyBlock _block;

        public float HoldDuration => _holdDuration;

        private void Awake() => ShowState();

        public void Bind(ConditionLock conditionLock) => _lock = conditionLock;

        public string GetInteractLabel(GameObject interactor) => _label;

        public bool CanInteract(GameObject interactor) => !_isOn && _lock != null;

        public void Interact(GameObject interactor)
        {
            if (_isOn || _lock == null) return;

            _isOn = true;
            _lock.Satisfy(this);
            ShowState();
        }

        private void ShowState()
        {
            if (_indicator == null) return;
            _block ??= new MaterialPropertyBlock();
            _indicator.GetPropertyBlock(_block);
            _block.SetColor(ColorId, _isOn ? _onColor : _offColor);
            _indicator.SetPropertyBlock(_block);
        }
    }
}
