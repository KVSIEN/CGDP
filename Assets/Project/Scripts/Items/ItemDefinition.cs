using UnityEngine;
using CGD.Core;

namespace CGD.Items
{
    // Shared identity for everything the player can hold.
    //
    // Items split into two families and this base carries only what both share.
    // Stackable items (resources, consumables, munitions, attachments) inherit
    // directly and have no per-instance state at all: a stack is a definition plus
    // a count, and a higher-quality variant is a *different definition* with a
    // higher Tier, never a better roll. Gear inherits GearDefinition instead.
    public abstract class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string  _displayName = "Item";
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite  _icon;
        [SerializeField] private Reality _reality = Reality.Neutral;
        [SerializeField] private ItemTier _tier = ItemTier.Common;
        [Tooltip("Weight per unit (kg). Stackables multiply by stack count; gear counts once.")]
        [SerializeField, Min(0f)] private float _weight = 0.1f;

        public string   DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public string   Description => _description;
        public Sprite   Icon        => _icon;
        public Reality  Reality     => _reality;
        public ItemTier Tier        => _tier;
        public float    Weight      => _weight;

        // Stackables override this. Gear stays at 1 because every piece is unique.
        public virtual int MaxStack => 1;

        public bool IsStackable => MaxStack > 1;
    }
}
