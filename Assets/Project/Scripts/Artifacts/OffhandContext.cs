using UnityEngine;
using CGD.Abilities;
using CGD.Combat;
using CGD.Meters;
using CGD.Stats;

namespace CGD.Artifacts
{
    // What an offhand artifact's use can reach on the player. PlayerOffhand fills it once and
    // updates the per-frame fields; any part the player lacks is null, and a use that needs it
    // simply can't be used.
    public class OffhandContext
    {
        public Transform      Player;
        public Transform      Camera;
        public MeterSet       Meters;
        public CharacterStats Stats;
        public Reflector      Reflector;
        // The player's ability context and events: a held tome casts through them, so perks,
        // combos and reloads see its casts like any other.
        public PlayerAbilities Abilities;

        // Game time and the frame's delta.
        public float Now;
        public float DeltaTime;
        // How well the held artifact rolled, 1 = average: scales the strength of what it does.
        public float Potency = 1f;
    }
}
