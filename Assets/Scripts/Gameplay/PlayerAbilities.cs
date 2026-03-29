using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private Ability[] _slots = new Ability[4];
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private Transform _cameraTransform;

    // Read by AbilityHUD to draw cooldown overlays
    public Ability[] Slots        => _slots;
    public float[]   CooldownTimers => _cooldownTimers;

    private PlayerInputHandler _input;
    private float[] _cooldownTimers;
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
        _input          = GetComponent<PlayerInputHandler>();
        _cooldownTimers = new float[_slots.Length];

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
            if (_cooldownTimers[i] > 0f)
                _cooldownTimers[i] -= Time.deltaTime;

            if (_slots[i] == null)                         continue;
            if (!_input.WasPressed(SlotActions[i]))        continue;
            if (_cooldownTimers[i] > 0f)                   continue;

            if (_slots[i].Execute(_ctx))
                _cooldownTimers[i] = _slots[i].Cooldown;
        }
    }

    // Returns 0 (just used) to 1 (ready). Used by AbilityHUD to size the overlay.
    public float GetReadyRatio(int slot)
    {
        if (_slots[slot] == null || _cooldownTimers[slot] <= 0f) return 1f;
        return Mathf.Clamp01(1f - _cooldownTimers[slot] / _slots[slot].Cooldown);
    }
}
