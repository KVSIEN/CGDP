using UnityEngine;

[CreateAssetMenu(fileName = "NewGrenade", menuName = "CGD/Grenade Data")]
public class GrenadeData : ScriptableObject
{
    [Header("Identity")]
    public string GrenadeName = "Frag Grenade";
    public GameObject GrenadePrefab;

    [Header("Throw")]
    public float ThrowForce = 18f;
    [Tooltip("Starting and maximum number carried.")]
    public int MaxCarried = 3;

    [Header("Explosion")]
    [Tooltip("Seconds after being thrown before it detonates.")]
    public float FuseTime = 2.5f;
    public float ExplosionRadius = 5f;
    public float ExplosionDamage = 80f;
    [Range(0f, 1f)] public float ArmorPenetration = 0f;
    public DamageType DamageType = DamageType.Physical;
    public LayerMask HitMask = ~0;

    [Header("Audio")]
    public SoundBank ThrowSound;
    public SoundBank ExplosionSound;

    [Header("Trajectory Preview")]
    public int   TrajectorySteps   = 30;
    public float TrajectoryStepTime = 0.1f;
    public Color DebugColor = Color.yellow;
}
