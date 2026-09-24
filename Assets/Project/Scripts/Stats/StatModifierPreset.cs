using UnityEngine;
using CGD.Items;

namespace CGD.Stats
{
    // A named group of stat modifiers applied together: a difficulty level ("Hard:
    // enemies +50% health, +25% damage"), a buff an ability grants, a zone's aura.
    [CreateAssetMenu(fileName = "StatModifierPreset", menuName = "CGD/Stats/Stat Modifier Preset")]
    public class StatModifierPreset : ScriptableObject
    {
        [SerializeField] private StatModifier[] _modifiers = System.Array.Empty<StatModifier>();

        public StatModifier[] Modifiers => _modifiers;
    }
}
