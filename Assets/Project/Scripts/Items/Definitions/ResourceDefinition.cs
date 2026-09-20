using UnityEngine;

namespace CGD.Items
{
    // Crafting material. Carries no per-instance state: quality is expressed by
    // having a separate, higher-Tier definition (oak is Common, ironwood is its own
    // asset at a higher tier), so recipes can match on identity alone and never have
    // to average quality across a mixed input pile.
    [CreateAssetMenu(fileName = "NewResource", menuName = "CGD/Items/Resource")]
    public class ResourceDefinition : ItemDefinition
    {
        [Header("Resource")]
        [SerializeField] private ResourceCategory _category = ResourceCategory.Raw;
        [SerializeField] private int _maxStack = 999;

        public ResourceCategory Category => _category;

        public override int MaxStack => Mathf.Max(1, _maxStack);
    }
}
