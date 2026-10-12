using System;
using UnityEngine;

namespace CGD.Weapons
{
    // One way weapons of a category reload, picked per weapon by Weight (see WeaponCategoryData):
    // a magazine, or rounds in steps (a pump shotgun's shells, an old bolt-action's rounds).
    // Names lists the models that load this way, so the rolled name always fits the reload.
    [Serializable]
    public class ReloadStyle
    {
        [Min(0f)] public float Weight = 1f;
        [Tooltip("Rounds loaded per reload step: 0 = the whole magazine at once; 1 = one at a time; 2, 3… = that many")]
        [Min(0)] public int RoundsPerLoad;
        [Tooltip("Models that load this way (e.g. AA-12 for a magazine shotgun). Empty = the category's Names")]
        public string[] Names = Array.Empty<string>();
    }
}
