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
    // away); holding Attack aims along the predicted arc — for a Charged grenade the longer the
    // further it goes (up to its charge time), for a Fixed one always the same distance — and
    // releasing throws one from the inventory. Aim Down Sights, a weapon key or readying it again puts it away;
    // after a throw the weapon comes back unless Keep Ready After Throw is set.
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerInventory))]
    public class ThrowableController : MonoBehaviour
    {
        private static readonly GameAction[] WeaponKeys = { GameAction.Weapon1, GameAction.Weapon2, GameAction.Weapon3, GameAction.Weapon4 };

        [SerializeField] private PlayerCamera _camera;
        [Tooltip("Stay ready for another throw while any are left, instead of going back to the weapon")]
        [SerializeField] private bool _keepReadyAfterThrow;
        [Tooltip("Draws the arc and blast ring while aiming")]
        [SerializeField] private TrajectoryPreview _preview;

        private PlayerInputHandler _input;
        private PlayerInventory    _inventory;
        private PlayerMovement     _movement;
        private WeaponController   _weapon;
        private Collider           _ownerCollider;
        private DamageSource       _damageSource;
        private bool               _aiming;
        private float              _heldTime;

        public ThrowableDefinition Readied { get; private set; }
        public bool IsAiming => _aiming;

        // How far the current throw is charged, 0..1 (0 when not aiming).
        public float Charge => _aiming && Readied != null ? Readied.Grenade.Charge(_heldTime) : 0f;

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
            StopAiming();
            if (_weapon != null) _weapon.SetHolstered(true);
            ReadiedChanged?.Invoke();
            return true;
        }

        public void PutAway()
        {
            if (Readied == null) return;
            Readied = null;
            StopAiming();
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
                StopAiming();
                return;
            }

            if (_input.IsHeld(GameAction.Attack))
            {
                if (!_aiming) _heldTime = 0f;
                _aiming = true;
                _heldTime += Time.deltaTime;
                if (_preview != null) _preview.Show(ThrowOrigin, LaunchVelocity(Readied.Grenade), Readied.Grenade);
            }
            else if (_aiming)
            {
                ThrowableDefinition throwable = Readied;
                Vector3 velocity = LaunchVelocity(throwable.Grenade);
                StopAiming();
                Throw(throwable, velocity);
            }
        }

        private void StopAiming()
        {
            _aiming = false;
            _heldTime = 0f;
            if (_preview != null) _preview.Hide();
        }

        // Aim, charge and the thrower's own movement decide the launch.
        private Vector3 LaunchVelocity(GrenadeData data)
        {
            Vector3 mover = _movement != null ? _movement.Velocity : Vector3.zero;
            return data.LaunchVelocity(_camera.transform.forward, _camera.transform.right, data.Charge(_heldTime), mover);
        }

        private bool WeaponKeyPressed()
        {
            foreach (GameAction key in WeaponKeys)
                if (_input.WasPressed(key)) return true;
            return false;
        }

        private Vector3 ThrowOrigin => _camera.transform.position + _camera.transform.forward * 0.5f;

        private void Throw(ThrowableDefinition throwable, Vector3 velocity)
        {
            GrenadeData data = throwable.Grenade;
            if (data.GrenadePrefab == null || !_inventory.Inventory.Remove(throwable, 1)) return;

            var go = PrefabPool.Spawn(data.GrenadePrefab, ThrowOrigin, Quaternion.identity);

            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity  = velocity;
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
    }
}
