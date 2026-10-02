using UnityEngine;

namespace CGD.Map
{
    // A part of the ship a stretch of the run passes through — Habitation, Commerce,
    // Engineering. The generator lays sections out in order from Start to the Boss; room
    // functions can prefer a section so each part of the run looks like where it is.
    [CreateAssetMenu(fileName = "MapSection", menuName = "CGD/Map/Section")]
    public class MapSectionDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Section";
        [Tooltip("Map Graph window colour")]
        [SerializeField] private Color _color = Color.gray;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;
    }
}
