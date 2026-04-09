using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponLoadout : MonoBehaviour
{
    [SerializeField] private WeaponData[] _slots = new WeaponData[4];

    public WeaponData[] Slots     => _slots;
    public int          ActiveSlot => _activeSlot;

    private PlayerInputHandler _input;
    private WeaponController   _weapon;
    private int                _activeSlot = -1;

    private static readonly GameAction[] SlotActions =
    {
        GameAction.Weapon1,
        GameAction.Weapon2,
        GameAction.Weapon3,
        GameAction.Weapon4,
    };

    private void Awake()
    {
        _input  = GetComponent<PlayerInputHandler>();
        _weapon = GetComponent<WeaponController>();
    }

    private void Start()
    {
        EquipSlot(0);
    }

    private void Update()
    {
        for (int i = 0; i < SlotActions.Length; i++)
        {
            if (_input.GetAction(SlotActions[i]))
                EquipSlot(i);
        }
    }

    public void EquipSlot(int index)
    {
        if (index < 0 || index >= _slots.Length) return;
        if (_activeSlot == index) return;

        _activeSlot = index;
        _weapon.Equip(_slots[index]);
    }

    public void SetSlot(int index, WeaponData data)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index] = data;
        if (_activeSlot == index)
            _weapon.Equip(data);
    }
}
