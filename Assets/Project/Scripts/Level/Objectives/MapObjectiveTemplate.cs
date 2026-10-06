using System;
using UnityEngine;

namespace CGD.Level
{
    // A kind of map objective: a few steps, each in a room of a given tier, done in order or
    // in any order. Escalating ones climb through tiers 1 → 2 → 3; themed ones keep every
    // step in rooms of one tier (or one faction). A map rolls its objectives from these.
    [CreateAssetMenu(fileName = "MapObjective", menuName = "CGD/Level/Map Objective")]
    public class MapObjectiveTemplate : ScriptableObject
    {
        [SerializeField] private string _title = "Objective";
        [SerializeField, TextArea] private string _description;
        [SerializeField] private ObjectiveUse _use = ObjectiveUse.Either;
        [SerializeField, Min(0f)] private float _weight = 1f;

        [Header("Steps")]
        [SerializeField] private ObjectiveStepTemplate[] _steps = Array.Empty<ObjectiveStepTemplate>();
        [Tooltip("Steps unlock one at a time, in order. Otherwise all count at once")]
        [SerializeField] private bool _sequential = true;
        [Tooltip("Every step in rooms of the same faction")]
        [SerializeField] private bool _sameFaction;

        public string Title       => string.IsNullOrEmpty(_title) ? name : _title;
        public string Description => _description;
        public ObjectiveUse Use   => _use;
        public float  Weight      => _weight;
        public ObjectiveStepTemplate[] Steps => _steps;
        public bool   Sequential  => _sequential;
        public bool   SameFaction => _sameFaction;

        public bool CanBe(bool main) => _use == ObjectiveUse.Either || _use == (main ? ObjectiveUse.Main : ObjectiveUse.Side);
    }
}
