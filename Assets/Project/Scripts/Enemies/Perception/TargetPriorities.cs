using UnityEngine;

namespace CGD.Enemies
{
    // Whom an enemy would rather fight when it senses more than one hostile — a faction's
    // temperament in a crowded fight (a predator goes for the weakest, a soldier shoots back
    // at whoever is shooting it, an opportunist picks off whoever is busy with someone else).
    // Set on EnemyData › Target Priorities; empty = only sticks with its current target.
    // Only matters where an enemy has several hostiles to choose from (rival factions).
    [CreateAssetMenu(fileName = "TargetPriorities", menuName = "CGD/Enemies/Target Priorities")]
    public class TargetPriorities : ScriptableObject
    {
        [Tooltip("×this for the hostile it is already fighting: another must be clearly better to make it switch")]
        [SerializeField, Min(1f)] private float _focus = 1.25f;
        [Tooltip("×(1 + this) for whoever hurt it in the last Retaliation Memory seconds")]
        [SerializeField, Min(0f)] private float _retaliation;
        [Tooltip("Seconds a hit keeps the attacker marked for retaliation")]
        [SerializeField, Min(0f)] private float _retaliationMemory = 4f;
        [Tooltip("×(1 + this × share of health and shield missing): a hostile at half strength with 1 counts 1.5×")]
        [SerializeField, Min(0f)] private float _wounded;
        [Tooltip("×(1 + this) for a hostile it sees fighting someone else (another faction's enemy that is engaged elsewhere; never the player)")]
        [SerializeField, Min(0f)] private float _distracted;
        [Tooltip("Focus fire: ×(1 + this × share of health and shield missing) for the hostile it is already fighting, " +
                 "so the more it has worn one down the more it sees it through. With 3, a target at a third left counts 3×; " +
                 "a fresh one hardly at all, so it still turns on a bigger threat")]
        [SerializeField, Min(0f)] private float _finish;

        public TargetWeights Weights           => new(_focus, _retaliation, _wounded, _distracted, _finish);
        public float         RetaliationMemory => _retaliationMemory;
    }
}
