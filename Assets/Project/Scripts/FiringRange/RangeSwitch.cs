using UnityEngine;
using CGD.Interaction;

namespace CGD.FiringRange
{
    // A button on the firing line: using it steps one setting of the range to its next value.
    [RequireComponent(typeof(Collider))]
    public class RangeSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private RangeControl _range;
        [SerializeField] private RangeSetting _setting;

        public bool CanInteract(GameObject interactor) => _range != null;

        public string GetInteractLabel(GameObject interactor) => _range.Describe(_setting);

        public void Interact(GameObject interactor) => _range.Cycle(_setting);
    }
}
