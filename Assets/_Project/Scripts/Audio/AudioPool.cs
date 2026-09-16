using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPool : MonoBehaviour
{
    public static AudioPool Instance { get; private set; }

    [SerializeField] private int _initialSize = 16;

    private readonly Stack<AudioSource> _available = new();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < _initialSize; i++)
            _available.Push(CreateSource());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
    {
        if (clip == null) return;

        var source = Get();
        source.transform.position = position;
        source.clip         = clip;
        source.volume       = volume;
        source.pitch        = pitch;
        source.spatialBlend = spatialBlend;
        source.Play();

        StartCoroutine(ReturnWhenDone(source, clip.length / Mathf.Max(Mathf.Abs(pitch), 0.01f)));
    }

    private AudioSource Get()
    {
        return _available.Count > 0 ? _available.Pop() : CreateSource();
    }

    private AudioSource CreateSource()
    {
        var go  = new GameObject("PooledAudio");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake  = false;
        src.spatialBlend = 1f;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.minDistance   = 1f;
        src.maxDistance   = 50f;
        return src;
    }

    private IEnumerator ReturnWhenDone(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration + 0.05f);
        source.Stop();
        source.clip = null;
        _available.Push(source);
    }
}
