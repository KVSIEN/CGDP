using UnityEngine;

// Add alongside WeaponPickup. Assign a WeaponCategoryData asset to define what
// weapon type this pickup represents. On Awake, stats are randomly generated
// within the category's thresholds and applied to the WeaponPickup.
[RequireComponent(typeof(WeaponPickup))]
public class RandomWeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponCategoryData _category;

    private void Awake()
    {
        if (_category == null) return;
        GetComponent<WeaponPickup>().SetData(WeaponGenerator.Generate(_category));
    }
}
