using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // An agent's working memory, shared by every node of its tree: facts one branch
    // learns ("has retreated", "cover point") that another branch reads. One per agent;
    // keys are plain names. Reading a missing key returns the fallback.
    public sealed class Blackboard
    {
        private readonly Dictionary<string, BlackboardValue> _values = new();

        public IReadOnlyDictionary<string, BlackboardValue> Values => _values;

        public bool Has(string key) => _values.ContainsKey(key);

        public void Remove(string key) => _values.Remove(key);

        public void Clear() => _values.Clear();

        public void SetBool(string key, bool value)       => _values[key] = BlackboardValue.Of(value);
        public void SetInt(string key, int value)         => _values[key] = BlackboardValue.Of(value);
        public void SetFloat(string key, float value)     => _values[key] = BlackboardValue.Of(value);
        public void SetVector(string key, Vector3 value)  => _values[key] = BlackboardValue.Of(value);
        public void SetObject(string key, Object value)   => _values[key] = BlackboardValue.Of(value);

        public bool    GetBool(string key, bool fallback = false)        => TryGet(key, BlackboardValueType.Bool,   out var v) ? v.Bool   : fallback;
        public int     GetInt(string key, int fallback = 0)              => TryGet(key, BlackboardValueType.Int,    out var v) ? v.Int    : fallback;
        public float   GetFloat(string key, float fallback = 0f)         => TryGet(key, BlackboardValueType.Float,  out var v) ? v.Float  : fallback;
        public Vector3 GetVector(string key, Vector3 fallback = default) => TryGet(key, BlackboardValueType.Vector, out var v) ? v.Vector : fallback;
        public Object  GetObject(string key)                             => TryGet(key, BlackboardValueType.Object, out var v) ? v.Object : null;

        private bool TryGet(string key, BlackboardValueType type, out BlackboardValue value) =>
            _values.TryGetValue(key, out value) && value.Type == type;
    }
}
