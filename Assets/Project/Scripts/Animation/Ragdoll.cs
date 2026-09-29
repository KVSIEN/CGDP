using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Animation
{
    // Swaps a character's animated pose for physics when it dies, knocked away from the
    // last hit, and puts the pose back when it's revived or reused from the pool.
    //
    // The bones are the rig's child Rigidbodies (usually the same colliders its Hitboxes
    // use). While alive they stay kinematic and follow the animation; on death they're
    // handed to physics and the Animator stops.
    [RequireComponent(typeof(HealthManager))]
    public class Ragdoll : MonoBehaviour, IPoolable
    {
        [Tooltip("Stopped on death so it doesn't fight the physics")]
        [SerializeField] private Animator _animator;
        [Tooltip("Empty = every Rigidbody under this object, except one on this object itself")]
        [SerializeField] private Rigidbody[] _bones;
        [Tooltip("Colliders that only matter while alive (the movement capsule) — turned off on death")]
        [SerializeField] private Collider[] _disableOnDeath = System.Array.Empty<Collider>();
        [Tooltip("Impulse on the bone nearest the killing blow, away from the attacker")]
        [SerializeField, Min(0f)] private float _deathImpulse = 6f;

        private HealthManager _health;
        private Vector3[]     _localPositions;
        private Quaternion[]  _localRotations;

        private void Awake()
        {
            _health = GetComponent<HealthManager>();
            if (_bones == null || _bones.Length == 0) _bones = FindBones();

            _localPositions = new Vector3[_bones.Length];
            _localRotations = new Quaternion[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                _localPositions[i] = _bones[i].transform.localPosition;
                _localRotations[i] = _bones[i].transform.localRotation;
            }

            SetRagdoll(false);
            _health.OnDeath   += OnDeath;
            _health.OnRevived += Restore;
        }

        private void OnDestroy()
        {
            _health.OnDeath   -= OnDeath;
            _health.OnRevived -= Restore;
        }

        public void OnSpawned() => Restore();

        private void OnDeath()
        {
            SetRagdoll(true);

            Rigidbody nearest = NearestBone(_health.LastHitPoint);
            if (nearest == null) return;

            Vector3 from = _health.LastHitSource.Owner != null ? _health.LastHitSource.Owner.transform.position : transform.position - transform.forward;
            Vector3 direction = _health.LastHitPoint - from;
            direction.y = Mathf.Max(direction.y, 0f);
            if (direction.sqrMagnitude < 0.0001f) direction = -transform.forward;

            nearest.AddForceAtPosition(direction.normalized * _deathImpulse, _health.LastHitPoint, ForceMode.Impulse);
        }

        private void Restore()
        {
            SetRagdoll(false);
            for (int i = 0; i < _bones.Length; i++)
                _bones[i].transform.SetLocalPositionAndRotation(_localPositions[i], _localRotations[i]);
        }

        private void SetRagdoll(bool active)
        {
            foreach (Rigidbody bone in _bones)
            {
                bone.isKinematic = !active;
                bone.interpolation = active ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            }

            foreach (Collider collider in _disableOnDeath)
                if (collider != null) collider.enabled = !active;

            if (_animator != null) _animator.enabled = !active;
        }

        private Rigidbody NearestBone(Vector3 point)
        {
            Rigidbody nearest = null;
            float best = float.MaxValue;
            foreach (Rigidbody bone in _bones)
            {
                float distance = (bone.worldCenterOfMass - point).sqrMagnitude;
                if (distance >= best) continue;
                best    = distance;
                nearest = bone;
            }
            return nearest;
        }

        private Rigidbody[] FindBones()
        {
            Rigidbody[] all = GetComponentsInChildren<Rigidbody>(true);
            int count = 0;
            foreach (Rigidbody body in all)
                if (body.gameObject != gameObject) all[count++] = body;
            System.Array.Resize(ref all, count);
            return all;
        }
    }
}
