using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyAudio : MonoBehaviour
{
    [SerializeField] private SoundBank _hurtSound;
    [SerializeField] private SoundBank _deathSound;

    private EnemyHealth _health;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        _health.OnDamaged += OnDamaged;
        _health.OnDeath   += OnDeath;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= OnDamaged;
        _health.OnDeath   -= OnDeath;
    }

    private void OnDamaged(float health, float maxHealth) => _hurtSound?.Play(transform.position);
    private void OnDeath()                                => _deathSound?.Play(transform.position);
}
