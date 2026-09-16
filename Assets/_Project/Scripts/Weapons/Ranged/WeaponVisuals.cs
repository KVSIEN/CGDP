using UnityEngine;

/// <summary>
/// Attach to the weapon pivot (child of the camera).
/// Applies procedural spring-based kick on fire that visually matches camera recoil.
/// Call AddKick() from WeaponController each time a shot is fired.
/// </summary>
public class WeaponVisuals : MonoBehaviour
{
    [Header("Kick")]
    [SerializeField] private float _kickPitchScale   = 8f;    // degrees of local pitch per unit of camera recoil
    [SerializeField] private float _kickZPushback    = 0.04f; // metres pushed back along local Z on fire
    [SerializeField] private float _kickRollScale    = 0.4f;  // roll per unit of horizontal kick

    [Header("Spring")]
    [SerializeField] private float _rotationStiffness = 18f;
    [SerializeField] private float _rotationDamping   = 6f;
    [SerializeField] private float _positionStiffness = 22f;
    [SerializeField] private float _positionDamping   = 7f;

    // Spring state — in local space relative to rest
    private Vector3 _rotVelocity;
    private Vector3 _rotOffset;
    private Vector3 _posVelocity;
    private Vector3 _posOffset;

    /// <summary>
    /// Called by WeaponController on every shot.
    /// vertKick and horizKick are the same values passed to PlayerCamera.AddRecoil.
    /// </summary>
    public void AddKick(float vertKick, float horizKick)
    {
        _rotOffset.x -= vertKick  * _kickPitchScale;  // pitch up (negative X = muzzle rise in local space)
        _rotOffset.z -= horizKick * _kickRollScale;   // roll with horizontal drift
        _posOffset.z -= _kickZPushback;               // push back
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        // Spring rotation offset back to zero
        _rotOffset   = SpringDamp(_rotOffset, Vector3.zero, ref _rotVelocity, _rotationStiffness, _rotationDamping, dt);
        _posOffset   = SpringDamp(_posOffset, Vector3.zero, ref _posVelocity, _positionStiffness, _positionDamping, dt);

        transform.localRotation = Quaternion.Euler(_rotOffset);
        transform.localPosition = _posOffset;
    }

    private static Vector3 SpringDamp(Vector3 current, Vector3 target, ref Vector3 velocity,
                                       float stiffness, float damping, float dt)
    {
        Vector3 force = (target - current) * stiffness - velocity * damping;
        velocity += force * dt;
        return current + velocity * dt;
    }
}
