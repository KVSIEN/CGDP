using System;
using UnityEngine;

namespace CGD.Map
{
    // A room type the content asks for in one specific place (see MapContentSettings).
    [Serializable]
    public class MapRoomGuarantee
    {
        [SerializeField] private bool        _enabled;
        [Tooltip("Start, Boss and Exit can't be guaranteed — the layout places them")]
        [SerializeField] private MapNodeType _type = MapNodeType.Treasure;

        // Used by the Inspector when the field is first serialized.
        public MapRoomGuarantee() { }

        public MapRoomGuarantee(bool enabled, MapNodeType type)
        {
            _enabled = enabled;
            _type    = type;
        }

        public bool        Enabled => _enabled && !_type.IsStructural();
        public MapNodeType Type    => _type;
    }
}
