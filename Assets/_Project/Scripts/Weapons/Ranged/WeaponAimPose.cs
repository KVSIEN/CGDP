using UnityEngine;
using CGD.Player;

namespace CGD.Weapons
{
    /// <summary>
    /// Moves the weapon model between its hip pose (the local position/rotation it has in the
    /// scene) and an aim-down-sights pose, following PlayerCamera's ADS blend. Sits on the
    /// weapon model under WeaponRig, whose transform WeaponVisuals owns for kick, so the aim
    /// offset and the kick stack instead of overwriting each other.
    /// </summary>
    public class WeaponAimPose : MonoBehaviour
    {
        [SerializeField] private PlayerCamera _camera;

        [Tooltip("Local position while fully aiming. Tune in Play mode so the sights line up with the screen centre.")]
        [SerializeField] private Vector3 _adsPosition = new(0f, -0.08f, 0.35f);
        [Tooltip("Local rotation (euler angles) while fully aiming")]
        [SerializeField] private Vector3 _adsRotation;

        private Vector3    _hipPosition;
        private Quaternion _hipRotation;

        private void Awake()
        {
            _hipPosition = transform.localPosition;
            _hipRotation = transform.localRotation;
        }

        private void LateUpdate()
        {
            // AdsT moves linearly; easing it makes the raise into and out of the sights settle softly.
            float t = Mathf.SmoothStep(0f, 1f, _camera.AdsT);
            transform.localPosition = Vector3.Lerp(_hipPosition, _adsPosition, t);
            transform.localRotation = Quaternion.Slerp(_hipRotation, Quaternion.Euler(_adsRotation), t);
        }
    }
}
