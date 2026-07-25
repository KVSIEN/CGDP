using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceDatabase", menuName = "CGD/Surface Database")]
public class SurfaceDatabase : ScriptableObject
{
    [Serializable]
    public class SurfaceEntry
    {
        public PhysicMaterial Material;
        public SoundBank Walk;
        public SoundBank Sprint;
        public SoundBank Crouch;
    }

    [SerializeField] private SurfaceEntry[] _surfaces;

    [Header("Fallback (used when no PhysicMaterial matches)")]
    [SerializeField] private SoundBank _defaultWalk;
    [SerializeField] private SoundBank _defaultSprint;
    [SerializeField] private SoundBank _defaultCrouch;

    public void GetBanks(PhysicMaterial mat, out SoundBank walk, out SoundBank sprint, out SoundBank crouch)
    {
        if (mat != null && _surfaces != null)
        {
            for (int i = 0; i < _surfaces.Length; i++)
            {
                if (_surfaces[i].Material == mat)
                {
                    walk   = _surfaces[i].Walk;
                    sprint = _surfaces[i].Sprint;
                    crouch = _surfaces[i].Crouch;
                    return;
                }
            }
        }

        walk   = _defaultWalk;
        sprint = _defaultSprint;
        crouch = _defaultCrouch;
    }

    public void GetDefaults(out SoundBank walk, out SoundBank sprint, out SoundBank crouch)
    {
        walk   = _defaultWalk;
        sprint = _defaultSprint;
        crouch = _defaultCrouch;
    }
}
