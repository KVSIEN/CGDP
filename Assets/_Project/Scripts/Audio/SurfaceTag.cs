using UnityEngine;

public class SurfaceTag : MonoBehaviour
{
    [SerializeField] private SoundBank _walk;
    [SerializeField] private SoundBank _sprint;
    [SerializeField] private SoundBank _crouch;

    public SoundBank Walk   => _walk;
    public SoundBank Sprint => _sprint;
    public SoundBank Crouch => _crouch;
}
