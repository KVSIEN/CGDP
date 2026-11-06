using UnityEngine;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Animation
{
    // Drives the first-person weapon model's Animator. Every weapon shares one Animator
    // Controller (the state machine: idle, fire, reload, draw, sprint…) and brings its own
    // clips through an AnimatorOverrideController on its WeaponData — the standard way to
    // animate many weapons without a controller each. A weapon without overrides plays the
    // controller's own clips; one without a rig is unaffected, as the animation is cosmetic.
    // Lives on the WeaponRig; the Animator and AnimationEventRelay sit on the model.
    public class ViewmodelAnimator : MonoBehaviour
    {
        [Tooltip("Animator on the weapon model. Its controller is the shared base")]
        [SerializeField] private Animator _animator;
        [SerializeField] private WeaponController _weapons;
        [Tooltip("Optional — drives the Aim parameter")]
        [SerializeField] private PlayerCamera _camera;
        [Tooltip("Optional — drives the Sprinting parameter")]
        [SerializeField] private PlayerMovement _movement;
        [Tooltip("Optional — receives the drawn weapon's cue sounds")]
        [SerializeField] private AnimationCueAudio _cueAudio;

        private AnimatorBridge            _bridge;
        private RuntimeAnimatorController _baseController;

        private void Awake()
        {
            _bridge = new AnimatorBridge(_animator);
            if (_animator != null) _baseController = _animator.runtimeAnimatorController;
        }

        // Also runs when the rig is shown again after a holster, so the weapon is drawn again.
        private void OnEnable()
        {
            if (_weapons == null) return;

            _weapons.Equipped += Refresh;
            _weapons.Fired    += OnFired;
            _weapons.DryFired += OnDryFired;
            Refresh();
        }

        private void OnDisable()
        {
            if (_weapons == null) return;

            _weapons.Equipped -= Refresh;
            _weapons.Fired    -= OnFired;
            _weapons.DryFired -= OnDryFired;
        }

        private void Update()
        {
            if (!_bridge.IsActive) return;

            _bridge.SetBool(AnimatorParams.Reloading, _weapons != null && _weapons.IsReloading);
            _bridge.SetBool(AnimatorParams.Sprinting, _movement != null && _movement.IsSprinting);
            if (_camera != null) _bridge.SetFloat(AnimatorParams.Aim, _camera.AdsT);
        }

        private void Refresh()
        {
            WeaponData data = _weapons.Data;
            ApplyController(data != null ? data.ViewmodelAnimations : null);
            if (_cueAudio != null) _cueAudio.SetSounds(data != null ? data.CueSounds : null);
            _bridge.SetTrigger(AnimatorParams.Equip);
        }

        private void ApplyController(RuntimeAnimatorController overrides)
        {
            RuntimeAnimatorController target = overrides != null ? overrides : _baseController;
            if (_animator == null || target == null || _animator.runtimeAnimatorController == target) return;

            _animator.runtimeAnimatorController = target;
            _bridge.Rebuild();
        }

        private void OnFired()    => _bridge.SetTrigger(AnimatorParams.Fire);
        private void OnDryFired() => _bridge.SetTrigger(AnimatorParams.DryFire);
    }
}
