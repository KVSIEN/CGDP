using System;
using UnityEngine;

namespace CGD.Level
{
    // How much each RoomTrait matters in a faction's fight rooms. Design targets: the Room Lab
    // measures every room against them and says where one falls short or overshoots.
    [Serializable]
    public class RoomTraitPriorities
    {
        [Tooltip("Floor to hide in: foliage and concealment patches")]
        [SerializeField] private TraitPriority _concealment = TraitPriority.Medium;
        [Tooltip("Narrow floor hemmed in by walls, pillars and furniture: corridors, aisles, corners to break sight")]
        [SerializeField] private TraitPriority _walls = TraitPriority.Medium;
        [Tooltip("Long open lanes with hard cover beside them to shoot from")]
        [SerializeField] private TraitPriority _linesOfFire = TraitPriority.Medium;

        public RoomTraitPriorities() { }

        public RoomTraitPriorities(TraitPriority concealment, TraitPriority walls, TraitPriority linesOfFire)
        {
            _concealment = concealment;
            _walls       = walls;
            _linesOfFire = linesOfFire;
        }

        public TraitPriority Of(RoomTrait trait) => trait switch
        {
            RoomTrait.Concealment => _concealment,
            RoomTrait.Walls       => _walls,
            _                     => _linesOfFire,
        };
    }
}
