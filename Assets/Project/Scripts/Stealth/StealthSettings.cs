using UnityEngine;

namespace CGD.Stealth
{
    // How hard a hidden character is to spot. One shared asset.
    [CreateAssetMenu(fileName = "StealthSettings", menuName = "CGD/Stealth/Stealth Settings")]
    public class StealthSettings : ScriptableObject
    {
        [Tooltip("A hidden character is still detected by any enemy this close (metres)")]
        [SerializeField, Min(0f)] private float _revealDistance = 2.5f;
        [Tooltip("…or this close when the enemy is inside the same bush or smoke")]
        [SerializeField, Min(0f)] private float _sharedCoverRevealDistance = 6f;

        [Header("Movement")]
        [SerializeField, Range(0.1f, 1f)] private float _crouchMultiplier = 0.6f;
        [SerializeField, Range(1f, 3f)]   private float _sprintMultiplier = 1.5f;

        [Header("Noticeability (everyone, hidden or not)")]
        [Tooltip("How fast enemies notice you while crouched, versus standing")]
        [SerializeField, Range(0.1f, 1f)] private float _crouchVisibility = 0.6f;
        [Tooltip("How fast enemies notice you while standing still, versus moving")]
        [SerializeField, Range(0.1f, 1f)] private float _stillVisibility = 0.6f;
        [Tooltip("How far crouched footsteps carry, as a share of an enemy's hearing range")]
        [SerializeField, Range(0f, 1f)] private float _crouchFootsteps = 0.3f;
        [Tooltip("How far sprinting footsteps carry, as a multiple of an enemy's hearing range")]
        [SerializeField, Range(1f, 3f)] private float _sprintFootsteps = 1.6f;

        [Header("Breaking stealth")]
        [Tooltip("Seconds a concealed character stays visible after attacking or making noise")]
        [SerializeField, Min(0f)] private float _revealAfterNoise = 1.5f;
        [Tooltip("Seconds 'spotted' lingers after the last enemy saw you")]
        [SerializeField, Min(0f)] private float _spottedMemory = 0.3f;

        public float RevealDistance            => _revealDistance;
        public float SharedCoverRevealDistance => _sharedCoverRevealDistance;
        public float CrouchMultiplier          => _crouchMultiplier;
        public float SprintMultiplier          => _sprintMultiplier;
        public float CrouchVisibility          => _crouchVisibility;
        public float StillVisibility           => _stillVisibility;
        public float CrouchFootsteps           => _crouchFootsteps;
        public float SprintFootsteps           => _sprintFootsteps;
        public float RevealAfterNoise          => _revealAfterNoise;
        public float SpottedMemory             => _spottedMemory;

        // How close an enemy must be to detect a hidden character.
        public float RevealRange(bool sharedCover, bool crouching, bool sprinting)
        {
            float range = sharedCover ? _sharedCoverRevealDistance : _revealDistance;
            if (crouching) range *= _crouchMultiplier;
            if (sprinting) range *= _sprintMultiplier;
            return range;
        }
    }
}
