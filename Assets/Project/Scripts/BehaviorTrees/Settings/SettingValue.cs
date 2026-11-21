using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A value given to a behavior's setting by name. Stored as a number: whole numbers and
    // choices are rounded, and an on/off flag is on for anything but 0.
    [Serializable]
    public struct SettingValue
    {
        [SerializeField] private string _name;
        [SerializeField] private float  _value;

        public SettingValue(string name, float value)
        {
            _name  = name;
            _value = value;
        }

        public string Name  => _name;
        public float  Value => _value;

        // The value `values` gives `setting`, for the nodes and tunings that each keep such a list.
        public static bool TryFind(IReadOnlyList<SettingValue> values, string setting, out float value)
        {
            foreach (SettingValue entry in values)
            {
                if (entry._name != setting) continue;
                value = entry._value;
                return true;
            }
            value = 0f;
            return false;
        }

        internal static void Set(List<SettingValue> values, string setting, float value)
        {
            Clear(values, setting);
            values.Add(new SettingValue(setting, value));
        }

        internal static void Clear(List<SettingValue> values, string setting) =>
            values.RemoveAll(entry => entry._name == setting);
    }
}
