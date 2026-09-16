using UnityEngine;

namespace CGD.Combat
{
    // Required prefab setup: Rigidbody (isKinematic = true), Collider (isTrigger = true).
    // ProjectileAbility.Execute configures Physics.IgnoreCollision so the projectile
    // passes through the player who fired it.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        public float Speed    { get; set; } = 25f;
        public float Lifetime { get; set; } = 5f;
        public float Damage   { get; set; } = 25f;
        public float CriticalMultiplier { get; set; } = 1f;

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start() => Destroy(gameObject, Lifetime);

        private void Update()
        {
            _rb.MovePosition(_rb.position + transform.forward * Speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            Hitbox.ApplyHit(other, new DamageInfo(Damage, criticalMultiplier: CriticalMultiplier), transform.position);
            Destroy(gameObject);
        }
    }
}
