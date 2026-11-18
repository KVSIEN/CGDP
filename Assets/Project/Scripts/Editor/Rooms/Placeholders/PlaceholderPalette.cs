using System.Collections.Generic;
using UnityEngine;

namespace CGD.Editor
{
    // The placeholder kit's colours by role (ROOM_GENERATION.md › Placeholder kit). The
    // Placeholder Prop Builder only offers these, and PlaceholderKitTests holds every prop to
    // them, so a room's colours mean the same thing wherever it is.
    [CreateAssetMenu(fileName = "PlaceholderPalette", menuName = "CGD/Level/Placeholder Palette")]
    public class PlaceholderPalette : ScriptableObject
    {
        public const string DefaultPath = "Assets/Project/Data/Level/Placeholders/DefaultPlaceholderPalette.asset";

        [SerializeField] private List<PlaceholderSwatch> _swatches = new();

        public IReadOnlyList<PlaceholderSwatch> Swatches => _swatches;

        // Index of the swatch that uses `material`, or -1.
        public int IndexOf(Material material)
        {
            for (int i = 0; i < _swatches.Count; i++)
                if (_swatches[i] != null && _swatches[i].Material == material) return i;
            return -1;
        }

        public Material MaterialAt(int index) =>
            index >= 0 && index < _swatches.Count && _swatches[index] != null ? _swatches[index].Material : null;

        public GUIContent[] Labels()
        {
            var labels = new GUIContent[_swatches.Count];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = _swatches[i] != null ? new GUIContent(_swatches[i].Name, _swatches[i].Use) : new GUIContent("(empty)");
            return labels;
        }
    }
}
