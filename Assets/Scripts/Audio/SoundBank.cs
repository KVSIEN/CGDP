using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundBank", menuName = "CGD/Sound Bank")]
public class SoundBank : ScriptableObject
{
    [SerializeField] private AudioClip[] _clips;
    [SerializeField] private float _minPitch  = 0.95f;
    [SerializeField] private float _maxPitch  = 1.05f;
    [SerializeField] private float _minVolume = 0.9f;
    [SerializeField] private float _maxVolume = 1f;
    [Tooltip("0 = 2D (no spatialization), 1 = full 3D")]
    [Range(0f, 1f)]
    [SerializeField] private float _spatialBlend = 1f;

    public void Play(Vector3 position)
    {
        if (_clips == null || _clips.Length == 0 || AudioPool.Instance == null) return;

        var clip = _clips[Random.Range(0, _clips.Length)];
        if (clip == null) return;

        float pitch  = Random.Range(_minPitch, _maxPitch);
        float volume = Random.Range(_minVolume, _maxVolume);
        AudioPool.Instance.Play(clip, position, volume, pitch, _spatialBlend);
    }

    public void Play(Vector3 position, float volumeScale)
    {
        if (_clips == null || _clips.Length == 0 || AudioPool.Instance == null) return;

        var clip = _clips[Random.Range(0, _clips.Length)];
        if (clip == null) return;

        float pitch  = Random.Range(_minPitch, _maxPitch);
        float volume = Random.Range(_minVolume, _maxVolume) * volumeScale;
        AudioPool.Instance.Play(clip, position, volume, pitch, _spatialBlend);
    }
}
