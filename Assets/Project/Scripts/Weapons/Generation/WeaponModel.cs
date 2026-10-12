using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Weapons
{
    // One real-world model a category rolls (an AK-47, an AUG), picked per weapon by Weight
    // (see WeaponCategoryData): how it shoots, where it sits in the category's stat ranges
    // and how it reloads, so every weapon of that name plays like that gun.
    [Serializable]
    public class WeaponModel
    {
        public string Name;
        [Min(0f)] public float Weight = 1f;
        [Tooltip("Ways this model shoots, by weight. A variant can go by another name (the M16's auto variant is an M4A1). Empty = the category's Firing Styles or Fire Modes")]
        public FiringStyle[] FiringStyles = Array.Empty<FiringStyle>();
        [Tooltip("Where this model sits in the category's stat ranges, e.g. an AK-47 hits harder and fires slower")]
        public StyleScales Scales = StyleScales.Identity;
        [Tooltip("Which way its stats tend inside those ranges, so the model usually plays like itself (an M14 tends to hit hard and fire slowly)")]
        public StyleLeans Leans;
        [Tooltip("Rounds loaded per reload step: 0 = the whole magazine at once; 1 = one at a time; 2, 3… = that many")]
        [Min(0)] public int RoundsPerLoad;
        [Tooltip("Ammo this model fires, e.g. Heavy Rounds for a magnum revolver. None = the category's")]
        public AmmoType AmmoType;
    }
}
