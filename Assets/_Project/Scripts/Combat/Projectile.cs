using UnityEngine;

namespace CGD.Combat
{
    // Required prefab setup: Rigidbody (isKinematic = true), Collider (isTrigger = true).
    // Passes through trigger volumes (pickups, interaction zones) and its owner's own
    // colliders; stops on the first solid collider it enters.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        public float Speed    { get; set; } = 25f;
        public float Lifetime { get; set; } = 5f;
        public float Damage   { get; set; } = 25f;
        public float CriticalMultiplier { get; set; } = 1f;
        public DamageSource Source { get; set; }
        public StatusEffectApplication[] OnHitEffects { get; set; }

        private Rigidbody _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start() => Destroy(gameObject, Lifetime);

        private void FixedUpdate()
        {
            _rb.MovePosition(_rb.position + transform.forward * (Speed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            if (Source.Owner != null && other.transform.root == Source.Owner.transform.root) return;

            var info = new DamageInfo(Damage, criticalMultiplier: CriticalMultiplier, source: Source, onHitEffects: OnHitEffects);
            Hitbox.ApplyHit(other, info, transform.position);
            Destroy(gameObject);
        }
    }
}
