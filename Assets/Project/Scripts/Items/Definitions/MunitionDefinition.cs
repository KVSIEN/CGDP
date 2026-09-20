using UnityEngine;

namespace CGD.Items
{
    // A stack of ammunition. The AmmoType is what weapons draw against, so bullets
    // are shared across every ballistic weapon rather than being tied to one gun.
    [CreateAssetMenu(fileName = "NewMunition", menuName = "CGD/Items/Munition")]
    public class MunitionDefinition : ItemDefinition
    {
        [Header("Munition")]
        [SerializeField] private AmmoType _ammoType = AmmoType.StandardRounds;
        [SerializeField] private int _maxStack = 999;

        public AmmoType AmmoType => _ammoType;

        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
