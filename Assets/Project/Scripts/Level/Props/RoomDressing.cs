using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // What a room type is dressed with beyond its function's own furniture: cover in a fight,
    // crates in a vault. Shared by every room type that lists it, so one asset gives the
    // same look to rooms that should look alike (a Treasure room and the Ambush posing as one).
    [CreateAssetMenu(fileName = "RoomDressing", menuName = "CGD/Level/Room Dressing")]
    public class RoomDressing : ScriptableObject
    {
        [Tooltip("Placed in order after the room's function furniture: wall-bound and large things first, loose clutter last. Each one keeps every doorway and floor tile reachable")]
        [SerializeField] private List<RoomPropPlacement> _props = new();

        public IReadOnlyList<RoomPropPlacement> Props => _props;
    }
}
