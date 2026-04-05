using UnityEngine;

// Attach to a world GameObject with a Collider (set Is Trigger = true).
// Assign a WeaponData asset in the Inspector.
// When the player presses E nearby, the weapon fills the first empty loadout slot,
// or replaces the active slot if all four are already filled.
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponData _data;

    public string InteractLabel => _data != null ? $"Pick Up  {_data.WeaponName}" : "Pick Up";

    public void SetData(WeaponData data) => _data = data;

    public void Interact(GameObject player)
    {
        if (_data == null) return;

        var loadout = player.GetComponent<PlayerWeaponLoadout>();
        if (loadout == null) return;

        // Try to find an empty slot first
        for (int i = 0; i < loadout.Slots.Length; i++)
        {
            if (loadout.Slots[i] != null) continue;

            loadout.SetSlot(i, _data);
            loadout.EquipSlot(i);
            Destroy(gameObject);
            return;
        }

        // All slots full — replace the active slot
        int target = Mathf.Max(0, loadout.ActiveSlot);
        loadout.SetSlot(target, _data);
        Destroy(gameObject);
    }
}
