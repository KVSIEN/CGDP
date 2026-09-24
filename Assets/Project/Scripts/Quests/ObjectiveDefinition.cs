using System;
using UnityEngine;

namespace CGD.Quests
{
    [Serializable]
    public class ObjectiveDefinition
    {
        [SerializeField] private string        _description = "Objective";
        [SerializeField] private ObjectiveKind _kind;
        [Tooltip("EnemyData for Kill, ItemDefinition for Collect, QuestSignal for Signal")]
        [SerializeField] private UnityEngine.Object _target;
        [SerializeField, Min(1)] private int   _requiredCount = 1;
        [Tooltip("Doesn't block the quest from completing")]
        [SerializeField] private bool          _optional;

        public string        Description   => _description;
        public ObjectiveKind Kind          => _kind;
        public UnityEngine.Object Target   => _target;
        public int           RequiredCount => Mathf.Max(1, _requiredCount);
        public bool          IsOptional    => _optional;

        public bool Counts(in QuestEvent e) => e.Kind == _kind && e.Target == _target;
    }
}
