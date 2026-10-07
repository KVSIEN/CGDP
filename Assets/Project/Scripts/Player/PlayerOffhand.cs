using System;
using UnityEngine;
using CGD.Abilities;
using CGD.Artifacts;
using CGD.Combat;
using CGD.Input;
using CGD.Items;
using CGD.Meters;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.Player
{
    // What the active loadout slot's offhand does: a one-handed melee weapon, a shield or an
    // artifact (PlayerWeaponLoadout stores them). It only counts while the main hand is one-handed
    // (or empty); with a two-handed main the offhand stays in its slot, unused. Then:
    //   • the MeleeController uses a held weapon or shield on the Melee key (and a shield's guard);
    //   • an artifact's rolled stats and passive perks go onto CharacterStats, and its active use
    //     runs here on the aim input (right mouse): tap or hold, as its behavior says;
    //   • whatever takes the aim input (a raised shield, an active artifact) stops a gun aiming;
    //   • its main-hand penalty (spread, recoil…) goes onto CharacterStats too.
    [RequireComponent(typeof(PlayerWeaponLoadout))]
    public class PlayerOffhand : MonoBehaviour
    {
        [SerializeField] private PlayerCamera _camera;

        private readonly OffhandContext _context = new();

        private PlayerWeaponLoadout _loadout;
        private PlayerInputHandler  _input;
        private PlayerMovement      _movement;
        private PlayerDodge         _dodge;
        private MeleeController     _melee;
        private CombatActions       _actions;
        private HealthManager       _health;
        private CharacterStats      _stats;

        private ArtifactInstance _artifact;
        private OffhandUse       _use;
        private bool             _holding;

        // The active slot's offhand, in use or not.
        public ItemInstance Item => _loadout.ActiveOffhand;
        // Held, and the main hand leaves room for it.
        public bool IsActive { get; private set; }
        // The active artifact's use, for the HUD (null without one).
        public OffhandUse Use => _use;
        public ArtifactInstance Artifact => _artifact;

        // The held item, or whether it is in use, changed.
        public event Action Changed;

        public static bool CanHold(ItemInstance item) =>
            item is ShieldInstance || item is ArtifactInstance || (item is MeleeWeaponInstance melee && melee.IsOneHanded);

        private void Awake()
        {
            _input   = GetComponent<PlayerInputHandler>();
            _loadout = GetComponent<PlayerWeaponLoadout>();
            TryGetComponent(out _movement);
            TryGetComponent(out _dodge);
            TryGetComponent(out _melee);
            TryGetComponent(out _actions);
            TryGetComponent(out _health);
            _stats = GetComponentInParent<CharacterStats>();

            _context.Player    = transform;
            _context.Camera    = _camera != null ? _camera.transform : transform;
            _context.Stats     = _stats;
            _context.Abilities = GetComponent<PlayerAbilities>();
            _context.Reflector = GetComponent<Reflector>();
            _context.Meters    = GetComponent<MeterSet>();
        }

        private void OnEnable()
        {
            _loadout.Changed += Refresh;
            if (_actions != null) _actions.Performed += OnCombatAction;
            if (_health  != null) _health.OnRevived      += OnRevived;
            Refresh();
        }

        private void OnDisable()
        {
            _loadout.Changed -= Refresh;
            if (_actions != null) _actions.Performed -= OnCombatAction;
            if (_health  != null) _health.OnRevived      -= OnRevived;
            StopUse();
        }

        private void Update()
        {
            if (_use == null) return;

            FillFrame();
            _use.Tick(_context);

            if (!CanAct())
            {
                StopUse();
                return;
            }

            bool pressed = _input.WasPressed(GameAction.AimDownSights);
            if (_use.Mode == OffhandUseMode.Tap)
            {
                if (pressed) _use.Begin(_context);
                return;
            }

            if (pressed && !_holding) _holding = _use.Begin(_context);
            if (!_holding) return;

            if (!_input.IsHeld(GameAction.AimDownSights) || !_use.Hold(_context))
                StopUse();
        }

        private bool CanAct() =>
            (_movement == null || _movement.CanAct) && (_dodge == null || !_dodge.IsDrivingMovement);

        private void FillFrame()
        {
            _context.Now       = Time.time;
            _context.DeltaTime = Time.deltaTime;
            _context.Potency   = _artifact != null ? _artifact.Potency : 1f;
        }

        // Ends whatever the use is doing (a hold, a cast in progress).
        private void StopUse()
        {
            if (_use == null) return;

            FillFrame();
            _use.End(_context);
            _holding = false;
        }

        private void OnCombatAction()
        {
            if (_use == null) return;
            FillFrame();
            _use.OnCombatAction(_context);
        }

        private void OnRevived() => _use?.Reset();

        private void OnArtifactAttachmentsChanged()
        {
            WearerStats.Apply(_stats, _artifact);
            Changed?.Invoke();
        }

        // Re-derives everything that follows from what is held and whether the main hand allows it.
        private void Refresh()
        {
            StopUse();
            ReleaseArtifact();

            WeaponItem mainHand = _loadout.Active;
            ItemInstance held   = _loadout.ActiveOffhand;
            IsActive = held != null && (mainHand == null || mainHand.IsOneHanded);
            var offhand = IsActive ? (IOffhand)held : null;

            if (offhand is ArtifactInstance artifact) TakeUpArtifact(artifact);
            ApplyMainHandPenalty(offhand);

            if (_camera != null) _camera.AimTakenByOffhand = offhand != null && offhand.TakesAim;
            if (_melee != null) _melee.SetOffhand(offhand);
            Changed?.Invoke();
        }

        private void TakeUpArtifact(ArtifactInstance artifact)
        {
            _artifact = artifact;
            _use      = artifact.Use;
            artifact.AttachmentsChanged += OnArtifactAttachmentsChanged;
            WearerStats.Apply(_stats, artifact);
        }

        private void ReleaseArtifact()
        {
            if (_artifact == null) return;

            _artifact.AttachmentsChanged -= OnArtifactAttachmentsChanged;
            WearerStats.Remove(_stats, _artifact);
            _artifact = null;
            _use      = null;
        }

        private void ApplyMainHandPenalty(IOffhand offhand)
        {
            if (_stats == null) return;

            _stats.RemoveFrom(this);
            if (offhand == null) return;

            foreach (StatModifier modifier in offhand.MainHandPenalty)
                if (modifier.Stat != ItemStat.None)
                    _stats.Add(modifier.Stat, modifier.Op, modifier.Value, this);
        }
    }
}
