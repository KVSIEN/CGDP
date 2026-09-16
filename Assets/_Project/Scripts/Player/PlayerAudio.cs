using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private SoundBank  _hurtSound;
    [SerializeField] private SoundBank  _deathSound;

    private void OnEnable()
    {
        if (_stats == null) return;
        _stats.OnDamaged += PlayHurt;
        _stats.OnDeath   += PlayDeath;
    }

    private void OnDisable()
    {
        if (_stats == null) return;
        _stats.OnDamaged -= PlayHurt;
        _stats.OnDeath   -= PlayDeath;
    }

    private void PlayHurt(float damage) => _hurtSound?.Play(transform.position);
    private void PlayDeath()            => _deathSound?.Play(transform.position);
}
