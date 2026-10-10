using System;
using UnityEngine;

namespace CGD.Map
{
    // A node type that is always one category: the Exit is a docking bay, a Shop is commercial.
    [Serializable]
    public class TypeCategory
    {
        [SerializeField] private MapNodeType _type = MapNodeType.Exit;
        [SerializeField] private RoomCategory _category = RoomCategory.Transit;

        public MapNodeType  Type     => _type;
        public RoomCategory Category => _category;
    }
}
