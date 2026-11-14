using System;
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
    }
}
