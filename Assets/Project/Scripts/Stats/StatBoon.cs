using UnityEngine;

namespace CGD.Stats
{
    // A preset with a name to show the player: what a shrine offers or a gauntlet wave lays
    // on them. Mix positive and negative values for a blessing with a curse.
    [CreateAssetMenu(fileName = "StatBoon", menuName = "CGD/Stats/Boon")]
    public class StatBoon : StatModifierPreset
    {
        [SerializeField] private string _label;

        public string Label => string.IsNullOrEmpty(_label) ? name : _label;
    }
}
