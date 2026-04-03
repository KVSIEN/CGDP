using UnityEngine;

// Required prefab setup: Rigidbody (isKinematic = true), Collider (isTrigger = true).
// ProjectileAbility.Execute configures Physics.IgnoreCollision so the projectile
// passes through the player who fired it.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float Speed    = 25f;
    public float Lifetime = 5f;
    public float Damage   = 25f;

    private void Awake()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().isTrigger    = true;
    }

    private void Start() => Destroy(gameObject, Lifetime);

    private void Update()
    {
        transform.position += transform.forward * Speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerStats>(out var playerStats))
            playerStats.TakeDamage(Damage);
        else if (other.TryGetComponent<EnemyHealth>(out var enemyHealth))
            enemyHealth.TakeDamage(Damage);

        Destroy(gameObject);
    }
}
