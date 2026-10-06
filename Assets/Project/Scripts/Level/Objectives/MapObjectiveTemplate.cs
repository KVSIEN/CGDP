using System;
using UnityEngine;

namespace CGD.Level
{
    // A kind of map objective: a few steps, each in a room within a tier range, done in order
    // or in any order. The ranges and the tier order decide the shape: a fixed climb 1 → 2 → 3,
    // any combination that never goes down (1-1-3, 1-2-2…), every step in one tier, or one
    // faction's rooms. A map rolls its objectives (and their tiers) from these.
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
        [Tooltip("How the steps' room tiers relate, within each step's range")]
        [SerializeField] private ObjectiveTierOrder _tierOrder = ObjectiveTierOrder.Any;
        [Tooltip("Every step in rooms of the same faction")]
        [SerializeField] private bool _sameFaction;

        public string Title       => string.IsNullOrEmpty(_title) ? name : _title;
        public string Description => _description;
        public ObjectiveUse Use   => _use;
        public float  Weight      => _weight;
        public ObjectiveStepTemplate[] Steps => _steps;
        public bool   Sequential  => _sequential;
        public bool   SameFaction => _sameFaction;
        public ObjectiveTierOrder TierOrder => _tierOrder;

        public bool CanBe(bool main) => _use == ObjectiveUse.Either || _use == (main ? ObjectiveUse.Main : ObjectiveUse.Side);
    }
}
