using System;
using UnityEngine;
using CGD.Artifacts;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Loot
{
    // A container subtype ("Pistols", "Healing"): narrows what a loot roll may drop without
    // its own tables, so the shared pools stay the one list of what exists. Anything it
    // rejects is skipped and the rest share the odds; prefab entries never pass a filter.
    [CreateAssetMenu(fileName = "LootFilter", menuName = "CGD/Loot/Loot Filter")]
    public class LootFilter : ScriptableObject
    {
        [Tooltip("Shown after the container's prompt, e.g. \"Open weapon crate (Pistols)\"")]
        [SerializeField] private string _label;
        [SerializeField] private LootItemKinds _kinds;
        [Tooltip("Firearms of these types only. Empty = any firearm (when Firearm is allowed)")]
        [SerializeField] private WeaponType[] _weaponTypes = Array.Empty<WeaponType>();
        [Tooltip("Munitions of these types only, e.g. pistol ammo in a pistol crate. Empty = any munition (when Munition is allowed)")]
        [SerializeField] private AmmoType[] _ammoTypes = Array.Empty<AmmoType>();

        public string Label => _label;

        public bool Allows(ItemDefinition item)
        {
            if (item == null || (KindOf(item) & _kinds) == 0) return false;

            return item switch
            {
                WeaponCategoryData firearm  => _weaponTypes.Length == 0 || Array.IndexOf(_weaponTypes, firearm.Type) >= 0,
                MunitionDefinition munition => _ammoTypes.Length == 0 || Array.IndexOf(_ammoTypes, munition.AmmoType) >= 0,
                _                           => true,
            };
        }

        public static LootItemKinds KindOf(ItemDefinition item) => item switch
        {
            WeaponCategoryData   => LootItemKinds.Firearm,
            MeleeCategoryData    => LootItemKinds.Melee,
            ArmorDefinition      => LootItemKinds.Armor,
            ShieldDefinition     => LootItemKinds.Shield,
            ArtifactDefinition   => LootItemKinds.Artifact,
            AttachmentDefinition => LootItemKinds.Attachment,
            ConsumableDefinition consumable => consumable.Heal > 0f ? LootItemKinds.Healing : LootItemKinds.Buff,
            ThrowableDefinition  => LootItemKinds.Throwable,
            MunitionDefinition   => LootItemKinds.Munition,
            ResourceDefinition   => LootItemKinds.Resource,
            _                    => LootItemKinds.None,
        };
    }
}
