using UnityEngine;
using CGD.Interaction;

namespace CGD.FiringRange
{
    // A button on the firing line: using it steps one setting of the range to its next value.
    [RequireComponent(typeof(Collider), typeof(Renderer))]
    public class RangeSwitch : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private RangeControl _range;
        [SerializeField] private RangeSetting _setting;
        [Tooltip("Tint of the switch, so each setting is recognisable at a glance")]
        [SerializeField] private Color _color = Color.white;

        private void Awake()
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, _color);
            GetComponent<Renderer>().SetPropertyBlock(block);
        }

        public bool CanInteract(GameObject interactor) => _range != null;

        public string GetInteractLabel(GameObject interactor) => _range.Describe(_setting);

        public void Interact(GameObject interactor) => _range.Cycle(_setting, interactor);
    }
}
