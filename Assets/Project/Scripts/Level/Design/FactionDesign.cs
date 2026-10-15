using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Factions;

namespace CGD.Level
{
    // The design principles of one faction's architecture, applied to every room it holds:
    //   - floor plans: which shapes it favours and how big its rooms are
    //   - structure: the columns and walls it builds where a room's function brings none
    //   - dressing: its own props, its own version of the room types' cover and clutter, and
    //     how crowded it keeps a room
    // LevelBuildSettings lists one per faction; a faction without one is built plainly. In a
    // Breach room the first faction's design sets the building, and both bring their props.
    [CreateAssetMenu(fileName = "FactionDesign", menuName = "CGD/Level/Faction Design")]
    public class FactionDesign : ScriptableObject
    {
        [SerializeField] private FactionDefinition _faction;

        [Header("Floor plans")]
        [Tooltip("How much more or less often this faction's rooms use a floor plan. Shapes not listed keep their weight")]
        [SerializeField] private List<ShapePreference> _shapes = new();
        [Tooltip("Single-cell rooms of a function are this share of their usual width and depth (landmarks and multi-cell halls keep theirs)")]
        [SerializeField, Range(0.5f, 1.25f)] private float _roomSizeScale = 1f;

        [Header("Structure")]
        [Tooltip("Columns and walls, applied in order to rooms whose function has none of its own (a function with its own structure keeps it)")]
        [SerializeField] private RoomStructureRule[] _structure = Array.Empty<RoomStructureRule>();

        [Header("Dressing")]
        [Tooltip("The faction's own props, placed after the room type's dressing and the function's furniture")]
        [SerializeField] private RoomDressing _dressing;
        [Tooltip("Where a room type lists the Original dressing, this faction's rooms use the Replacement: the same cover or clutter, in the faction's own pieces")]
        [SerializeField] private List<DressingSwap> _swaps = new();
        [Tooltip("The share of a room's floor props may take: low for clean halls, high for crowded offices")]
        [SerializeField, Range(0.1f, 0.8f)] private float _fillShare = RoomPropPlanner.DefaultFillShare;
        [Tooltip("Multiplies how many of every prop a room gets (required ones excepted)")]
        [SerializeField, Min(0.1f)] private float _propScale = 1f;

        public FactionDefinition Faction => _faction;
        public float RoomSizeScale => _roomSizeScale;
        public IReadOnlyList<RoomStructureRule> Structure => _structure;
        public RoomDressing Dressing => _dressing;
        public float FillShare => _fillShare;
        public float PropScale => _propScale;

        public float ShapeWeight(RoomShape shape)
        {
            foreach (ShapePreference preference in _shapes)
                if (preference != null && preference.Shape == shape) return preference.Weight;
            return 1f;
        }

        // This faction's version of a room type's dressing, or the dressing itself.
        public RoomDressing Dress(RoomDressing original)
        {
            foreach (DressingSwap swap in _swaps)
                if (swap != null && swap.Original == original && swap.Replacement != null) return swap.Replacement;
            return original;
        }
    }
}
