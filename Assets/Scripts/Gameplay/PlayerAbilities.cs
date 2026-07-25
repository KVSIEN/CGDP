using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private Ability[] _slots = new Ability[4];
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private Transform _cameraTransform;

    // Read by AbilityHUD to draw cooldown overlays
    public Ability[] Slots => _slots;

    private PlayerInputHandler _input;
    private PlayerMovement _movement;
    private CooldownTimer[] _cooldowns;
    private AbilityContext _ctx;

    // Maps slot index to the matching GameAction
    private static readonly GameAction[] SlotActions =
    {
        GameAction.Ability1,
        GameAction.Ability2,
        GameAction.Ability3,
        GameAction.Ability4,
    };

    private void Awake()
    {
        _input     = GetComponent<PlayerInputHandler>();
        _movement  = GetComponent<PlayerMovement>();
        _cooldowns = new CooldownTimer[_slots.Length];

        _ctx = new AbilityContext
        {
            PlayerTransform  = transform,
            PlayerRigidbody  = GetComponent<Rigidbody>(),
            PlayerCollider   = GetComponent<Collider>(),
            CameraTransform  = _cameraTransform,
            Stats            = _stats,
        };
    }

    private void Update()
    {
        _ctx.MoveInput = _input.MoveInput;

        for (int i = 0; i < _slots.Length; i++)
        {
            _cooldowns[i].Tick(Time.deltaTime);

            if (_slots[i] == null)                 continue;
            if (!_input.GetAction(SlotActions[i])) continue;
            if (!_cooldowns[i].IsReady)             continue;
            if (!_movement.CanAct)                 continue;

            if (_slots[i].Execute(_ctx))
                _cooldowns[i].Start(_slots[i].Cooldown);
        }
    }

    // Read by AbilityHUD to size the cooldown overlay.
    public CooldownTimer GetCooldown(int slot) => _cooldowns[slot];
}
