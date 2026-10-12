using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Weapons
{
    // How a weapon of a styled category shoots: its fire mode and, for a pellet style, the
    // fire behavior and pellet cone. One is picked per weapon by Weight (see WeaponCategoryData).
    [Serializable]
    public class FiringStyle
    {
        [Tooltip("Put in front of the weapon's name, e.g. \"Burst\". Empty = nothing")]
        public string Name;
        [Tooltip("Optional: the model name this variant goes by instead, e.g. M4A1 for an auto M16. Empty = the model's own")]
        public string ModelName;
        [Min(0f)] public float Weight = 1f;
        public FireMode FireMode = FireMode.Semi;
        [Tooltip("Optional: replaces the category's fire behavior, e.g. Shotgun for a pellet spread")]
        public WeaponFireBehavior FireBehavior;
        [Tooltip("Pellets per shot (damage is per pellet). 0–0 keeps the category's")]
        public IntRange PelletCount;
        [Tooltip("Aimed cone. 0–0 keeps the category's — a pellet style needs one, or every pellet lands on the same point")]
        public FloatRange AdsSpreadDeg;
        public StyleScales Scales = StyleScales.Identity;
        [Tooltip("Rounds per reload step for this variant, e.g. a revolver's speedloader loading all six at once. 0 = the model's")]
        [Min(0)] public int RoundsPerLoad;
        [Tooltip("Use Recoil below instead of the model's pattern, for a variant that kicks unlike the model's main one (a semi Glock 17 off a full-auto Glock 18)")]
        public bool OwnRecoil;
        public RecoilPattern Recoil = new();
    }
}
