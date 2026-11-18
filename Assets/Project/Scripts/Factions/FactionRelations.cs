using System;
using UnityEngine;
using CGD.Combat;

namespace CGD.Factions
{
    // How the factions treat one another, read by every enemy that references it (EnemyData ›
    // Relations): whom each prefers to hunt, avoids or flees, and whether rivals leave each
    // other alone until something sets them off. Every team is still hostile to every other
    // one; this only decides who goes looking for which fight.
    [CreateAssetMenu(fileName = "FactionRelations", menuName = "CGD/Factions/Faction Relations")]
    public class FactionRelations : ScriptableObject
    {
        [Tooltip("Rival factions ignore each other while calm (unaware or merely suspicious) and only turn on one " +
                 "another once something has started a fight — the player, gunfire, a hit. Keeps rooms held by two " +
                 "factions intact until the player arrives. Never applies to the player")]
        [SerializeField] private bool _truceWhileCalm = true;
        [Tooltip("For any pair without a row below")]
        [SerializeField] private FactionAttitude _default = FactionAttitude.Fight;
        [SerializeField] private FactionRelation[] _relations = Array.Empty<FactionRelation>();

        [Header("Tuning")]
        [Tooltip("×this on how much it wants a hostile it hunts, over one it merely fights")]
        [SerializeField, Min(1f)] private float _huntWeight = 1.5f;
        [Tooltip("Morale lost per second for each feared hostile it senses in a fight")]
        [SerializeField, Min(0f)] private float _fearDrain = 0.15f;

        public float FearDrain => _fearDrain;

        public FactionAttitude AttitudeOf(Team own, Team toward)
        {
            foreach (FactionRelation relation in _relations)
                if (relation.From == own && relation.Toward == toward) return relation.Attitude;
            return _default;
        }

        // Whether a calm `own` leaves `other` alone: only rival factions, never the player.
        public bool KeepsTruce(Team own, Team other) =>
            _truceWhileCalm && other != own && other != Team.Player && other != Team.None;

        // How much it wants to fight a hostile it has this attitude toward. 0 = it won't start that fight.
        public float TargetWeight(FactionAttitude attitude) => attitude switch
        {
            FactionAttitude.Hunt  => _huntWeight,
            FactionAttitude.Fight => 1f,
            _                     => 0f,
        };
    }
}
