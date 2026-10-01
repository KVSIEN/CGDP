using System;
using UnityEngine;
using CGD.Audio;
using CGD.Combat;
using CGD.Core;
using CGD.Input;
using CGD.Player;

namespace CGD.Weapons
{
    // Holds a throwable in the player's hand. An item slot readies it (the gun is put
    // away); holding Attack aims along the predicted arc and releasing throws one from
    // the inventory. Aim Down Sights, a weapon key or readying it again puts it away;
    // after a throw the weapon comes back unless Keep Ready After Throw is set.
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerInventory))]
    public class ThrowableController : MonoBehaviour
    {
        private static readonly GameAction[] WeaponKeys = { GameAction.Weapon1, GameAction.Weapon2, GameAction.Weapon3, GameAction.Weapon4 };

        [SerializeField] private PlayerCamera _camera;
        [Tooltip("Stay ready for another throw while any are left, instead of going back to the weapon")]
        [SerializeField] private bool _keepReadyAfterThrow;
        [SerializeField] private bool _debugDrawTrajectory = true;

        private PlayerInputHandler _input;
        private PlayerInventory    _inventory;
        private PlayerMovement     _movement;
        private WeaponController   _weapon;
        private Collider           _ownerCollider;
        private DamageSource       _damageSource;
        private bool               _aiming;

        public ThrowableDefinition Readied { get; private set; }
        public bool IsAiming => _aiming;

        public event Action Thrown;
        public event Action ReadiedChanged;

        private void Awake()
        {
            _input     = GetComponent<PlayerInputHandler>();
            _inventory = GetComponent<PlayerInventory>();
            TryGetComponent(out _movement);
            TryGetComponent(out _weapon);
            _ownerCollider = GetComponent<Collider>();
            _damageSource  = DamageSource.Of(gameObject);
        }

        private void OnDisable() => PutAway();

        // Readying the one already in hand puts it away again.
        public bool Ready(ThrowableDefinition throwable)
        {
            if (Readied == throwable)
            {
                PutAway();
                return false;
            }
            if (throwable == null || throwable.Grenade == null || !_inventory.Inventory.Has(throwable, 1)) return false;

            Readied = throwable;
            _aiming = false;
            if (_weapon != null) _weapon.SetHolstered(true);
            ReadiedChanged?.Invoke();
            return true;
        }

        public void PutAway()
        {
            if (Readied == null) return;
            Readied = null;
            _aiming = false;
            if (_weapon != null) _weapon.SetHolstered(false);
            ReadiedChanged?.Invoke();
        }

        private void Update()
        {
            if (Readied == null) return;

            if (!_inventory.Inventory.Has(Readied, 1) || _input.WasPressed(GameAction.AimDownSights) || WeaponKeyPressed())
            {
                PutAway();
                return;
            }

            if (_movement != null && !_movement.CanAct)
            {
                _aiming = false;
                return;
            }

            if (_input.IsHeld(GameAction.Attack))
            {
                _aiming = true;
                if (_debugDrawTrajectory) DrawTrajectoryPreview(Readied.Grenade);
            }
            else if (_aiming)
            {
                _aiming = false;
                Throw(Readied);
            }
        }

        private bool WeaponKeyPressed()
        {
            foreach (GameAction key in WeaponKeys)
                if (_input.WasPressed(key)) return true;
            return false;
        }

        private Vector3 ThrowOrigin => _camera.transform.position + _camera.transform.forward * 0.5f;

        private void Throw(ThrowableDefinition throwable)
        {
            GrenadeData data = throwable.Grenade;
            if (data.GrenadePrefab == null || !_inventory.Inventory.Remove(throwable, 1)) return;

            var go = PrefabPool.Spawn(data.GrenadePrefab, ThrowOrigin, Quaternion.identity);

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity  = _camera.transform.forward * data.ThrowForce;
                rb.angularVelocity = Vector3.zero;
            }

            if (_ownerCollider != null && go.TryGetComponent<Collider>(out var col))
                Physics.IgnoreCollision(col, _ownerCollider);

            if (go.TryGetComponent<Grenade>(out var grenade))
                grenade.Init(data, _damageSource);

            data.ThrowSound.TryPlay(transform.position);
            Thrown?.Invoke();

            if (!_keepReadyAfterThrow || !_inventory.Inventory.Has(throwable, 1)) PutAway();
        }

        private void DrawTrajectoryPreview(GrenadeData data)
        {
            Vector3 pos = ThrowOrigin;
            Vector3 vel = _camera.transform.forward * data.ThrowForce;

            for (int i = 0; i < data.TrajectorySteps; i++)
            {
                Vector3 next = pos + vel * data.TrajectoryStepTime
                    + 0.5f * Physics.gravity * (data.TrajectoryStepTime * data.TrajectoryStepTime);

                if (Physics.Linecast(pos, next, out RaycastHit hit, data.HitMask, QueryTriggerInteraction.Ignore))
                {
                    Debug.DrawLine(pos, hit.point, data.DebugColor);
                    break;
                }

                Debug.DrawLine(pos, next, data.DebugColor);
                vel += Physics.gravity * data.TrajectoryStepTime;
                pos = next;
            }
        }
    }
}
