using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;
using CGD.Core;

namespace CGD.Enemies
{
    // Launches a survey beacon toward where the caster believes its target is (at most
    // MaxDeployDistance away). The beacon runs a fixed protocol each pulse — hostiles in range:
    // reveal them and report the nearest to every kin through walls; none: top up nearby allies'
    // shields — until its time is up or it is shot down. Logic anyone can learn and exploit:
    // the pulses are regular and visible, the beacon can be broken, and a player can spoof it.
    [CreateAssetMenu(fileName = "SurveyBeaconAbility", menuName = "CGD/Abilities/Survey Beacon")]
    public class SurveyBeaconAbility : Ability
    {
        [Tooltip("Pooled prefab with a SurveyBeacon and a DeployableHealth (Prefabs/Weapons/SurveyBeacon)")]
        public GameObject BeaconPrefab;
        [Tooltip("Metres from the caster it lands, toward its aim point, at most")]
        [Min(1f)] public float MaxDeployDistance = 14f;
        [Tooltip("Game seconds before it shuts down on its own")]
        [Min(1f)] public float Lifetime = 15f;
        [Tooltip("Seconds between pulses")]
        [Min(0.5f)] public float PulseInterval = 3f;
        public Color Color = new(1f, 0.85f, 0.2f, 0.6f);
        [Tooltip("Optional: Prefabs/VFX/ExplosionFlash, a ring shown on each pulse")]
        public GameObject PulseFlash;

        [Header("Scan")]
        [Tooltip("Metres it sees through walls on each pulse")]
        [Min(1f)] public float ScanRadius = 12f;
        [Tooltip("Seconds each pulse strips stealth (cover, cloaks) from hostiles in range")]
        [Min(0f)] public float RevealSeconds = 2.5f;
        [Tooltip("Metres a report reaches: every kin this close hears it, walls or not")]
        [Min(1f)] public float ReportRange = 80f;
        public LayerMask TargetMask = ~0;

        [Header("Repair (no hostiles in range)")]
        [Min(0f)] public float RepairRadius = 8f;
        [Tooltip("Shield given back to each ally in range per pulse")]
        [Min(0f)] public float RepairPerPulse = 15f;

        [Header("Spoof")]
        [Tooltip("Seconds the player must hold Interact on it to feed it a false position (0 = can't be spoofed)")]
        [Min(0f)] public float SpoofHoldSeconds = 2f;
        [Tooltip("Metres from the beacon, away from whoever spoofed it, where a spoofed beacon says hostiles are")]
        [Min(1f)] public float SpoofOffset = 15f;

        public override bool CanExecute(AbilityContext ctx) =>
            BeaconPrefab != null && BeaconPrefab.TryGetComponent<SurveyBeacon>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Transform caster = ctx.CasterTransform;
            Vector3 toward = ctx.HasAimPoint ? ctx.AimPoint - caster.position : caster.forward * MaxDeployDistance;
            toward.y = 0f;
            Vector3 spot = caster.position + Vector3.ClampMagnitude(toward, MaxDeployDistance) + Vector3.up;

            GameObject beacon = PrefabPool.Spawn(BeaconPrefab, OnFloor(spot, caster.position), Quaternion.identity);
            if (beacon.TryGetComponent(out SurveyBeacon survey)) survey.Deploy(this, ctx.Health.Team);
        }

        // On walkable floor near the spot (never inside a wall or on someone's head); at the
        // caster's feet when there is none.
        private static Vector3 OnFloor(Vector3 spot, Vector3 fallback) =>
            NavMesh.SamplePosition(spot, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : fallback;
    }
}
