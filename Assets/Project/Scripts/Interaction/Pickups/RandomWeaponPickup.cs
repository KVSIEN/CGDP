using UnityEngine;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Interaction
{
    [RequireComponent(typeof(WeaponPickup))]
    public class RandomWeaponPickup : MonoBehaviour, ISeedable
    {
        [Tooltip("Firearm or melee categories")]
        [SerializeField] private WeaponCategory[] _categories;
        [SerializeField] private int _fixedIndex = -1; // -1 = random
        [Tooltip("Same seed → same category pick and same weapon every time. Empty = random, or the level's seed when a generated level places it.")]
        [SerializeField] private string _seed;

        private void Awake() => Roll(string.IsNullOrWhiteSpace(_seed) ? Seed.Random() : Seed.Parse(_seed));

        // From whatever placed the pickup; a seed typed on the pickup itself wins.
        public void ApplySeed(Seed seed)
        {
            if (string.IsNullOrWhiteSpace(_seed)) Roll(seed.Derive("weapon"));
        }

        private void Roll(Seed seed)
        {
            if (_categories == null || _categories.Length == 0) return;

            WeaponCategory cat = _fixedIndex >= 0 && _fixedIndex < _categories.Length
                ? _categories[_fixedIndex]
                : seed.Derive("category").Stream().Pick(_categories);

            if (cat == null) return;
            GetComponent<WeaponPickup>().SetWeapon(cat.Generate(cat.Tier, seed));
        }
    }
}
