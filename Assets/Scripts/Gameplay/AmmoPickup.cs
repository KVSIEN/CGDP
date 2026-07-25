using UnityEngine;

// Attach to a world GameObject with a Collider (set Is Trigger = true).
// Adds reserve ammo to the player's currently equipped weapon on interact.
[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int _amount = 30;

    public string InteractLabel => "Pick Up  Ammo";

    public void Interact(GameObject player)
    {
        var weapon = player.GetComponent<WeaponController>();
        if (weapon == null) return;

        weapon.AddReserveAmmo(_amount);
        Destroy(gameObject);
    }
}
