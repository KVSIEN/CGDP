using System;
using UnityEngine;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Perks
{
    // Swaps the bash of the gun it's on for another quick melee — e.g. Tactical Knife's offhand
    // stab — and, as a passive, changes the gun too (the knife hand costs accuracy and recoil
    // control). Only fits firearms of the listed types, such as one-handed pistols and SMGs.
    [CreateAssetMenu(fileName = "QuickMeleePerk", menuName = "CGD/Perks/Quick Melee")]
    public class QuickMeleePerk : PassivePerk
    {
        [Tooltip("What the Melee key does instead of the gun's own bash; its start parries like any bash")]
        [SerializeField] private MeleeWeaponData _quickMelee;
        [Tooltip("Firearm categories it can roll on. Empty = any firearm")]
        [SerializeField] private WeaponType[] _weaponTypes = Array.Empty<WeaponType>();

        public MeleeWeaponData QuickMelee => _quickMelee;

        public override bool Fits(ItemInstance gear)
        {
            if (!base.Fits(gear) || gear is not WeaponInstance) return false;
            if (_weaponTypes.Length == 0) return true;
            return gear.Definition is WeaponCategoryData category && Array.IndexOf(_weaponTypes, category.Type) >= 0;
        }
    }
}
