using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private SoundBank  _hurtSound;
    [SerializeField] private SoundBank  _deathSound;

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDamaged += PlayHurt;
        _health.OnDeath   += PlayDeath;
    }

    private void OnDisable()
    {
        if (_health == null) return;
        _health.OnDamaged -= PlayHurt;
        _health.OnDeath   -= PlayDeath;
    }

    private void PlayHurt(float damage) => _hurtSound?.Play(transform.position);
    private void PlayDeath()            => _deathSound?.Play(transform.position);
}
