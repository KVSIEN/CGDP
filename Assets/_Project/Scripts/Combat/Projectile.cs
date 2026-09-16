using UnityEngine;
using CGD.Core;

namespace CGD.Combat
{
    // Required prefab setup: Rigidbody (isKinematic = true), Collider (isTrigger = true).
    // Spawn through PrefabPool and call Launch. Passes through trigger volumes (pickups,
    // interaction zones) and its owner's own colliders; stops on the first solid collider.
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        private Rigidbody  _rb;
        private DamageInfo _hit;
        private float      _speed;
        private float      _lifetime;
        private float      _age;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            GetComponent<Collider>().isTrigger = true;
        }

        // `hit` is dealt to whatever the projectile strikes; its Source is the shooter.
        public void Launch(DamageInfo hit, float speed, float lifetime)
        {
            _hit      = hit;
            _speed    = speed;
            _lifetime = lifetime;
            _age      = 0f;
        }

        private void FixedUpdate()
        {
            _age += Time.fixedDeltaTime;
            if (_age >= _lifetime)
            {
                PrefabPool.Release(gameObject);
                return;
            }

            _rb.MovePosition(_rb.position + transform.forward * (_speed * Time.fixedDeltaTime));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            GameObject owner = _hit.Source.Owner;
            if (owner != null && other.transform.root == owner.transform.root) return;

            Hitbox.ApplyHit(other, _hit, transform.position);
            PrefabPool.Release(gameObject);
        }
    }
}
