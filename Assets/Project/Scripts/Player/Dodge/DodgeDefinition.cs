using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Meters;

namespace CGD.Player
{
    // One kind of dodge — a sidestep that can become a roll, a committed roll, a steerable
    // boost, a long dash — as a sequence of stages. The player's PlayerDodge uses one.
    [CreateAssetMenu(fileName = "Dodge", menuName = "CGD/Player/Dodge")]
    public class DodgeDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Dodge";
        [Tooltip("Paid when the dodge starts (follow-up stages included). Leave Meter empty for free dodges")]
        [SerializeField] private MeterCost _cost;
        [SerializeField] private DodgeFallbackDirection _withoutInput = DodgeFallbackDirection.Backward;
        [Tooltip("Dodges allowed before landing again. 0 = on the ground only")]
        [SerializeField, Min(0)] private int _airDodges;
        [SerializeField] private List<DodgeStage> _stages = new() { new DodgeStage() };

        public string DisplayName => _displayName;
        public MeterCost Cost => _cost;
        public DodgeFallbackDirection WithoutInput => _withoutInput;
        public int AirDodges => _airDodges;
        public IReadOnlyList<DodgeStage> Stages => _stages;
    }
}
