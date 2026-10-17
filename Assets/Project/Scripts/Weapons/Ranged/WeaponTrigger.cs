using UnityEngine;
using CGD.Input;

namespace CGD.Weapons
{
    // What pulling the trigger asks the weapon to do this frame.
    public enum TriggerAction { None, Fire, FireBurst, Reload, DryFire }

    // Reads the attack button for the weapon in hand: which fire mode fires when, the buffered
    // press, the charge of a bow, and the click (or reload) of an empty magazine. It decides;
    // WeaponController does what it decides.
    public sealed class WeaponTrigger
    {
        private readonly InputBuffer _buffer = new();
        private float _chargeTimer;
        private bool  _wasHeld;

        // Seconds the trigger has been drawn, for the charge fire mode.
        public float ChargeTimer => _chargeTimer;

        // A press the gun couldn't act on yet (drawing, dodging, reloading, between shots) counts
        // as soon as it can. Callers only feed this when the player has input buffering on.
        public void BufferPress(float now) => _buffer.Press(now);

        // A swap must not carry someone else's charge or buffered press.
        public void Reset()
        {
            _chargeTimer = 0f;
            _wasHeld     = false;
            _buffer.Clear();
        }

        // Drops the current draw, as re-gripping a bow would.
        public void DropCharge() => _chargeTimer = 0f;

        // `charge` (0..1) is how drawn the shot is when the action is Fire.
        public TriggerAction Read(FireMode mode, float minChargeToFire, int magazine, bool canReload, bool burstPending,
                                  bool held, bool pressedNow, float now, float deltaTime, float chargeTime, out float charge)
        {
            charge = 1f;
            // Not short-circuited: a press made now must not leave an older buffered one behind.
            bool pressed = pressedNow | _buffer.Consume(now);

            if (magazine <= 0)
            {
                // Sync charge to trigger so post-reload hold doesn't instantly fire.
                _chargeTimer = 0f;
                _wasHeld     = held;
                if (burstPending || !(held || pressed)) return TriggerAction.None;
                if (canReload) return TriggerAction.Reload;
                return pressed ? TriggerAction.DryFire : TriggerAction.None;
            }

            switch (mode)
            {
                case FireMode.Auto  when held:    return TriggerAction.Fire;
                case FireMode.Semi  when pressed: return TriggerAction.Fire;
                case FireMode.Burst when pressed && !burstPending: return TriggerAction.FireBurst;
                case FireMode.Charge: return ReadCharge(minChargeToFire, held, deltaTime, chargeTime, out charge);
                default:              return TriggerAction.None;
            }
        }

        // Hold to charge, release to fire. Releasing before MinChargeToFire cancels the
        // shot with no ammo spent so tap-firing a bow doesn't waste arrows.
        private TriggerAction ReadCharge(float minChargeToFire, bool held, float deltaTime, float chargeTime, out float charge)
        {
            charge = 1f;
            TriggerAction action = TriggerAction.None;

            if (held)
            {
                _chargeTimer = Mathf.Min(_chargeTimer + deltaTime, chargeTime);
            }
            else if (_wasHeld)
            {
                charge = chargeTime > 0f ? Mathf.Clamp01(_chargeTimer / chargeTime) : 1f;
                if (charge >= minChargeToFire) action = TriggerAction.Fire;
                _chargeTimer = 0f;
            }
            _wasHeld = held;
            return action;
        }
    }
}
