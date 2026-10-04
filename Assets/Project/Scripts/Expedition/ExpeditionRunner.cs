using System;
using UnityEngine;
using CGD.Core;
using CGD.Flow;
using CGD.Input;
using CGD.Items;
using CGD.Level;
using CGD.Player;
using CGD.Weapons;

namespace CGD.Expedition
{
    // Runs one expedition in a level scene, between the ship (ExpeditionSession) and the
    // player: at the start it hands over what was packed on the ship plus the starting
    // room's random loadout; when GameFlow reaches Victory (the player extracted at the
    // Exit) or GameOver (the player died) it settles the run — everything carried goes to
    // the ship's hold, or is lost; through an Emergency Exit's escape pod only part of it
    // makes it (ExpeditionLedger.ExtractEmergency). Without a session (no GameFlow in the scene) it keeps a
    // ledger of its own so the scene still plays.
    [DefaultExecutionOrder(100)]   // after the player's own setup (starting stacks, loadout)
    public class ExpeditionRunner : MonoBehaviour
    {
        [SerializeField] private PlayerInventory     _inventory;
        [SerializeField] private PlayerWeaponLoadout _weapons;
        [SerializeField] private PlayerEquipment     _equipment;
        [Tooltip("Locked when the run ends, so the end screen can be used")]
        [SerializeField] private PlayerInputHandler  _input;
        [SerializeField] private RunStarterKit       _starterKit;
        [Tooltip("Optional — rolls the starting loadout from the level's seed, so a kept seed keeps its loadout")]
        [SerializeField] private LevelBuilder        _level;

        private ExpeditionLedger _localLedger;
        private GameFlow _flow;
        private bool _leftByEmergencyExit;

        public ExpeditionLedger Ledger =>
            ExpeditionSession.Instance != null ? ExpeditionSession.Instance.Ledger : _localLedger ??= new ExpeditionLedger();

        // Raised once the run is settled, with what it kept or cost.
        public event Action<RunReport> RunEnded;

        private void Start()
        {
            Give(Ledger.Deploy());
            GiveStarterKit();

            _flow = GameFlow.Instance;
            if (_flow != null) _flow.StateChanged += OnStateChanged;
            LevelExit.Used += OnExitUsed;
        }

        private void OnExitUsed(LevelExit exit) => _leftByEmergencyExit = exit.IsEmergency;

        private void OnDestroy()
        {
            if (_flow != null) _flow.StateChanged -= OnStateChanged;
            LevelExit.Used -= OnExitUsed;
            // Leaving mid-run (restart, main menu) is not making it out: the kit is gone.
            if (Ledger.IsRunning) Ledger.Die(new Inventory());
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (!Ledger.IsRunning) return;
            if (next != GameState.Victory && next != GameState.GameOver) return;

            RunReport report = next != GameState.Victory ? Ledger.Die(Carried())
                             : _leftByEmergencyExit   ? Ledger.ExtractEmergency(Worn(), Pack())
                             : Ledger.Extract(Carried());
            RunEnded?.Invoke(report);
            // After listeners: closing an open crafting window hands input back to the player.
            if (_input != null) _input.InputEnabled = false;
        }

        // Everything the player has on them: the pack, the weapon slots and worn armour.
        private Inventory Carried()
        {
            Inventory carried = Pack();
            ExpeditionLedger.TransferAll(Worn(), carried);
            return carried;
        }

        private Inventory Pack()
        {
            var pack = new Inventory();
            foreach (ItemStack stack in _inventory.Inventory.Stacks) pack.Add(stack.Definition, stack.Count);
            foreach (ItemInstance item in _inventory.Inventory.Items) pack.Add(item);
            return pack;
        }

        // Weapons in their slots and armour being worn.
        private Inventory Worn()
        {
            var worn = new Inventory();
            if (_weapons != null)
                foreach (WeaponItem weapon in _weapons.Slots)
                    if (weapon != null) worn.Add(weapon);
            if (_equipment != null)
                foreach (ItemInstance piece in _equipment.Equipment.Worn)
                    worn.Add(piece);
            return worn;
        }

        // Weapons fill free slots, armour is worn where its slot is free; the rest goes in the pack.
        private void Give(Inventory kit)
        {
            foreach (ItemStack stack in kit.Stacks)
                _inventory.Inventory.Add(stack.Definition, stack.Count);
            foreach (ItemInstance item in kit.Items)
                GiveItem(item);
        }

        private void GiveItem(ItemInstance item)
        {
            if (item is WeaponItem weapon && _weapons != null && HasFreeWeaponSlot())
            {
                _weapons.AddWeapon(weapon);
                return;
            }

            // Wear takes the armour out of the pack, so it goes in first; if it can't be
            // worn it simply stays there.
            _inventory.Inventory.Add(item);
            if (item.Definition is ArmorDefinition armor && _equipment != null && _equipment.Equipment.Get(armor.Slot) == null)
                _equipment.Wear(item);
        }

        private bool HasFreeWeaponSlot()
        {
            foreach (WeaponItem slot in _weapons.Slots)
                if (slot == null) return true;
            return false;
        }

        private void GiveStarterKit()
        {
            if (_starterKit == null) return;

            Seed seed = _level != null && _level.Graph != null ? _level.Seed.Derive("starter kit") : Seed.Random();
            RandomStream random = seed.Stream();

            foreach (WeaponCategoryData category in _starterKit.PickWeaponCategories(random))
                GiveItem(WeaponGenerator.Generate(category, random.NextSeed()));

            foreach (StarterSupply supply in _starterKit.Supplies)
            {
                int count = supply.Count.Evaluate(random);
                if (supply.Item != null && count > 0) _inventory.Inventory.Add(supply.Item, count);
            }
        }
    }
}
