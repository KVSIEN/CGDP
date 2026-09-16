using System;
using UnityEngine;

// Per-character-type damage multipliers for each hitbox region, shared by every
// HealthManager of that type (e.g. one profile for humanoids, one for armored heavies).
[CreateAssetMenu(fileName = "HitboxProfile", menuName = "CGD/Combat/Hitbox Profile")]
public class HitboxProfile : ScriptableObject
{
    [Serializable]
    private struct RegionEntry
    {
        public HitboxRegion Region;
        [Min(0f)] public float DamageMultiplier;
        [Tooltip("Critical hits also apply the attacker's DamageInfo.CriticalMultiplier (e.g. a weapon's headshot bonus)")]
        public bool IsCritical;
    }

    [SerializeField] private RegionEntry[] _regions =
    {
        new RegionEntry { Region = HitboxRegion.Head, DamageMultiplier = 1f, IsCritical = true },
        new RegionEntry { Region = HitboxRegion.Body, DamageMultiplier = 1f },
        new RegionEntry { Region = HitboxRegion.Limb, DamageMultiplier = 0.75f },
    };

    // A null profile or unlisted region falls back to ×1 with Head still critical,
    // so hitboxes work before any profile asset is authored.
    public static float Resolve(HitboxProfile profile, HitboxRegion region, float criticalMultiplier, out bool isCritical)
    {
        float multiplier = 1f;
        isCritical = region == HitboxRegion.Head;

        if (profile != null && profile.TryGetEntry(region, out RegionEntry entry))
        {
            multiplier = entry.DamageMultiplier;
            isCritical = entry.IsCritical;
        }

        return isCritical ? multiplier * criticalMultiplier : multiplier;
    }

    private bool TryGetEntry(HitboxRegion region, out RegionEntry entry)
    {
        foreach (RegionEntry candidate in _regions)
        {
            if (candidate.Region != region) continue;
            entry = candidate;
            return true;
        }

        entry = default;
        return false;
    }
}
