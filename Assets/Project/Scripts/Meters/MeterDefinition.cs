using UnityEngine;

namespace CGD.Meters
{
    // Configuration for one kind of consumable/regenerating resource: stamina, mana,
    // energy, rage, oxygen, battery charge... Shared by every character that has it.
    //
    // RegenRate's sign picks the resting direction: positive refills toward Max (stamina,
    // mana), negative decays toward 0 (rage, heat). RegenDelay pauses it whenever the
    // meter is pushed the other way — spending stamina, or gaining rage.
    [CreateAssetMenu(fileName = "NewMeter", menuName = "CGD/Meters/Meter Definition")]
    public class MeterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Meter";
        [SerializeField] private Color  _color = new Color(0.3f, 0.75f, 1f, 1f);

        [Header("Capacity")]
        [SerializeField, Min(0f)] private float _maxValue = 100f;
        [Tooltip("Fraction of Max the meter starts (and respawns) with")]
        [SerializeField, Range(0f, 1f)] private float _startRatio = 1f;

        [Header("Regeneration")]
        [Tooltip("Units per second. Positive refills, negative decays, 0 = static.")]
        [SerializeField] private float _regenRate = 20f;
        [Tooltip("Seconds regen waits after the meter moves against it (a spend when refilling, a gain when decaying)")]
        [SerializeField, Min(0f)] private float _regenDelay = 1f;

        [Header("Exhaustion")]
        [Tooltip("Once emptied, the meter can't be spent until it refills past this fraction of Max. 0 = no lockout.")]
        [SerializeField, Range(0f, 1f)] private float _exhaustionRecovery;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public Color  Color       => _color;

        public MeterSettings Settings => new(_maxValue, _regenRate, _regenDelay, _startRatio, _exhaustionRecovery);
    }
}
