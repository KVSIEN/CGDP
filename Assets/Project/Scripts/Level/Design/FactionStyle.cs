using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // A faction's design as one room shows it: at full strength in the core of its territory,
    // thinned toward a plain room on its frontier (TerritoryDepth). Every number moves from
    // its neutral value toward the design's by the strength; the design's additions and
    // dressing swaps happen with the strength as their chance. A style without a design is a
    // plain room.
    public readonly struct FactionStyle
    {
        public FactionStyle(FactionDesign design, float strength = 1f)
        {
            Design   = design;
            Strength = Mathf.Clamp01(strength);
        }

        public FactionDesign Design   { get; }
        public float         Strength { get; }
        public bool          HasDesign => Design != null;
        // At full strength nothing is drawn by chance, so the room builds as the design alone would.
        public bool          IsFull    => Strength >= 1f;

        public float RoomSizeScale => Toward(1f, Design != null ? Design.RoomSizeScale : 1f);
        public float FillShare     => Toward(RoomPropPlanner.DefaultFillShare, Design != null ? Design.FillShare : RoomPropPlanner.DefaultFillShare);
        public float PropScale     => Toward(1f, Design != null ? Design.PropScale : 1f);

        // A shape the design never uses stays unused; others move toward their plain weight.
        public float ShapeWeight(RoomShape shape)
        {
            float weight = Design != null ? Design.ShapeWeight(shape) : 1f;
            return weight <= 0f ? 0f : Toward(1f, weight);
        }

        public float ConcealmentScale(TraitPriority priority) => Toward(1f, RoomTraitFit.ConcealmentScale(priority));

        // The design's post preference, moved toward plain floor.
        public PostPreference Posts => Design == null ? null : IsFull ? Design.Posts : Design.Posts.Toward(Strength);

        // Whether a chance-based part of the design (an addition, a swap) shows in this room.
        public bool Shows(RandomStream random) => IsFull || random.Chance(Strength);

        private float Toward(float neutral, float full) => IsFull ? full : Mathf.Lerp(neutral, full, Strength);
    }
}
