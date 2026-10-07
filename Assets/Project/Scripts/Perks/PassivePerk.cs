using UnityEngine;
using CGD.Items;

namespace CGD.Perks
{
    // Always-on stat changes, e.g. +30% magazine, +10% crit chance, +8% move speed. On a weapon
    // they change the weapon itself, like an attachment would; on armor they change the wearer
    // while it's worn (see ItemInstance.PassivesAffectWearer).
    [CreateAssetMenu(fileName = "PassivePerk", menuName = "CGD/Perks/Passive")]
    public class PassivePerk : GearPerk
    {
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();

        public StatModifier[] Modifiers => _modifiers;
    }
}
