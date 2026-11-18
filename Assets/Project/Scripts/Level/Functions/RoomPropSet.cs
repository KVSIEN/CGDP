using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A kit of furnishings shared by several places and layouts — a control station facing the
    // machine, a run of pipes along the walls, a hazard cordon round the plant — so a kit is
    // tuned once and reads the same in every room that lists it. A set's placements go down
    // where the set is listed: a layout's sets after its own set pieces, a function's sets
    // after its own props.
    //
    // Near/Key work across sets: a set placed Near "Plant" stands by whatever the room keyed
    // "Plant" (every ship-operations place keys its main machine so; ROOM_GENERATION.md §12).
    [CreateAssetMenu(fileName = "RoomPropSet", menuName = "CGD/Level/Room Prop Set")]
    public class RoomPropSet : ScriptableObject
    {
        [Tooltip("What the kit is, for the Room Lab and the layout check (Control Station)")]
        [SerializeField] private string _displayName = "Prop Set";
        [SerializeField] private List<RoomPropPlacement> _props = new();

        public string DisplayName => _displayName;
        public IReadOnlyList<RoomPropPlacement> Props => _props;

        // Every placement of `sets`, in order; empty and missing sets are skipped.
        public static IEnumerable<RoomPropPlacement> PropsOf(IReadOnlyList<RoomPropSet> sets)
        {
            foreach (RoomPropSet set in sets)
            {
                if (set == null) continue;
                foreach (RoomPropPlacement placement in set._props) yield return placement;
            }
        }
    }
}
