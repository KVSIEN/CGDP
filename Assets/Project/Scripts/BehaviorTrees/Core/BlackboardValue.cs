using UnityEngine;

namespace CGD.BehaviorTrees
{
    // One blackboard entry. A struct holding each kind of value, so storing a float or a
    // position never boxes.
    public readonly struct BlackboardValue
    {
        public readonly BlackboardValueType Type;
        public readonly bool    Bool;
        public readonly int     Int;
        public readonly float   Float;
        public readonly Vector3 Vector;
        public readonly Object  Object;

        private BlackboardValue(BlackboardValueType type, bool b = false, int i = 0, float f = 0f,
            Vector3 v = default, Object o = null)
        {
            Type = type; Bool = b; Int = i; Float = f; Vector = v; Object = o;
        }

        public static BlackboardValue Of(bool value)    => new(BlackboardValueType.Bool,   b: value);
        public static BlackboardValue Of(int value)     => new(BlackboardValueType.Int,    i: value);
        public static BlackboardValue Of(float value)   => new(BlackboardValueType.Float,  f: value);
        public static BlackboardValue Of(Vector3 value) => new(BlackboardValueType.Vector, v: value);
        public static BlackboardValue Of(Object value)  => new(BlackboardValueType.Object, o: value);

        public override string ToString() => Type switch
        {
            BlackboardValueType.Bool   => Bool.ToString(),
            BlackboardValueType.Int    => Int.ToString(),
            BlackboardValueType.Float  => Float.ToString("0.##"),
            BlackboardValueType.Vector => Vector.ToString("F1"),
            _                          => Object != null ? Object.name : "None",
        };
    }
}
