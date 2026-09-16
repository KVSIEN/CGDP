using UnityEngine;

[RequireComponent(typeof(WeaponPickup))]
public class RandomWeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponCategoryData[] _categories;
    [SerializeField] private int _fixedIndex = -1; // -1 = random

    private void Awake()
    {
        if (_categories == null || _categories.Length == 0) return;

        WeaponCategoryData cat = _fixedIndex >= 0 && _fixedIndex < _categories.Length
            ? _categories[_fixedIndex]
            : _categories[Random.Range(0, _categories.Length)];

        if (cat == null) return;
        GetComponent<WeaponPickup>().SetData(WeaponGenerator.Generate(cat));
    }
}
