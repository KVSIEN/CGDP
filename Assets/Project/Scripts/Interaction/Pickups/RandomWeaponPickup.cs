using UnityEngine;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Interaction
{
    [RequireComponent(typeof(WeaponPickup))]
    public class RandomWeaponPickup : MonoBehaviour
    {
        [Tooltip("Firearm or melee categories")]
        [SerializeField] private WeaponCategory[] _categories;
        [SerializeField] private int _fixedIndex = -1; // -1 = random
        [Tooltip("Same seed → same category pick and same weapon every time. Empty = random.")]
        [SerializeField] private string _seed;

        private void Awake()
        {
            if (_categories == null || _categories.Length == 0) return;

            Seed seed = string.IsNullOrWhiteSpace(_seed) ? Seed.Random() : Seed.Parse(_seed);

            WeaponCategory cat = _fixedIndex >= 0 && _fixedIndex < _categories.Length
                ? _categories[_fixedIndex]
                : seed.Derive("category").Stream().Pick(_categories);

            if (cat == null) return;
            GetComponent<WeaponPickup>().SetWeapon(cat.Generate(cat.Tier, seed));
        }
    }
}
