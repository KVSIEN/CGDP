using System;
using System.Collections;
using UnityEngine;
using CGD.Items;
using CGD.Player;
using CGD.Stats;

namespace CGD.Weapons
{
    // The reload rules for the weapon in hand: how long it takes, when the rounds go in (the
    // commit point), and moving rounds from the reserve into the magazine. It owns the
    // reload's state but not the coroutine: WeaponController runs Run() and stops it when
    // something interrupts, then calls End() to settle the state.
    public sealed class WeaponReloader
    {
        private readonly PlayerInventory _inventory;
        private readonly CharacterStats  _stats;
        private readonly Transform       _soundOrigin;
        private readonly Action          _changed;

        private WeaponInstance _weapon;

        public WeaponReloader(PlayerInventory inventory, CharacterStats stats, Transform soundOrigin, Action changed)
        {
            _inventory   = inventory;
            _stats       = stats;
            _soundOrigin = soundOrigin;
            _changed     = changed;
        }

        public bool IsReloading { get; private set; }

        // The rounds are in; what is left is the tail of the animation.
        public bool IsCommitted { get; private set; }

        // A reload began; and its rounds went in (the commit point), whether or not it played out.
        public event Action Started;
        public event Action Committed;

        public int Reserve =>
            _inventory != null && _weapon != null ? _inventory.Inventory.CountOf(_weapon.Data.AmmoType) : 0;

        public bool CanReload => _weapon != null && _weapon.Magazine < _weapon.MagazineSize && Reserve > 0;

        private WeaponData D => _weapon.Data;

        public void SetWeapon(WeaponInstance weapon) => _weapon = weapon;

        // The reload coroutine for the weapon in hand.
        public IEnumerator Run() => D.LoadsInSteps ? ReloadInSteps() : Reload();

        // The rounds go in at the commit point; the rest is the tail of the animation, which
        // sprinting or a swap can skip. The magazine filling up is the cue that it's safe.
        private IEnumerator Reload()
        {
            Begin();

            float time   = Mathf.Max(0.1f, Wielder(ItemStat.ReloadTime,
                _weapon.Magazine > 0 ? _weapon.TacticalReloadTime : _weapon.ReloadTime));
            float commit = time * D.ReloadCommitPoint;
            yield return new WaitForSeconds(commit);

            LoadRounds(_weapon.MagazineSize - _weapon.Magazine, free: false);
            Commit();
            if (time > commit) yield return new WaitForSeconds(time - commit);

            End();
        }

        // RoundsPerLoad rounds at a time, each step in for good once its share of the reload
        // time is up, until the weapon is full or the reserve runs dry. The first step in is the
        // commit point (Committed fires once, as for a magazine). An interrupt keeps every step
        // already loaded, so a bash right after one cancels the rest and keeps it.
        private IEnumerator ReloadInSteps()
        {
            Begin();

            float perStep = Mathf.Max(0.05f, Wielder(ItemStat.ReloadTime, _weapon.ReloadTime) / D.LoadSteps(_weapon.MagazineSize));
            var wait = new WaitForSeconds(perStep);
            while (CanReload)
            {
                yield return wait;
                if (LoadRounds(D.RoundsPerLoad, free: false) == 0) break;
                if (!IsCommitted) Commit();
            }

            End();
        }

        private void Begin()
        {
            IsReloading = true;
            IsCommitted = false;
            _changed();

            D.ReloadSound.TryPlay(_soundOrigin.position);
            Started?.Invoke();
        }

        private void Commit()
        {
            IsCommitted = true;
            Committed?.Invoke();
        }

        // Loads up to `count` rounds into the weapon in hand, outside a reload too (perks use
        // this). Rounds come from the reserve unless `free`. Returns how many went in.
        public int LoadRounds(int count, bool free)
        {
            if (_weapon == null) return 0;

            int room      = _weapon.MagazineSize - _weapon.Magazine;
            int available = free ? room : Reserve;
            int taken     = Mathf.Min(Mathf.Min(count, room), available);
            if (taken <= 0) return 0;

            _weapon.Magazine += taken;
            // Remove fires Changed, which covers both mag and reserve in one event.
            if (!free) _inventory.Inventory.Remove(D.AmmoType, taken);
            else       _changed();
            return taken;
        }

        // Ends the reload where it is: finished if the rounds are in, lost if not.
        public void End()
        {
            Clear();
            _changed();
        }

        // Forgets the reload without announcing it (the weapon is being swapped or put away).
        public void Clear()
        {
            IsReloading = false;
            IsCommitted = false;
        }

        // A weapon value with the wielder's buffs and debuffs (e.g. a perk's faster reloads) applied.
        private float Wielder(ItemStat stat, float value) => _stats != null ? _stats.Apply(stat, value) : value;
    }
}
