using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // On a prop prefab that comes in several sizes: wherever the room planner places this prop,
    // it may put one of the other sizes there instead. Kept on the prop rather than in the room
    // data, so every room that places it gets the whole range. A variant must fit one tile, or
    // state the footprint it needs; its own list (if any) is not followed.
    [DisallowMultipleComponent]
    public class PropSizeVariants : MonoBehaviour
    {
        [Tooltip("How often this size itself stands, against the variants' weights")]
        [SerializeField, Min(0f)] private float _ownWeight = 1f;
        [SerializeField] private PropSizeVariant[] _variants = Array.Empty<PropSizeVariant>();

        public IReadOnlyList<PropSizeVariant> Variants => _variants;

        // This prop or one of its other sizes, by weight. Sizes that need a higher room, stand
        // lower than `minHeight` metres or don't pass `allowed` are left out; this prop itself
        // never is.
        public GameObject Pick(RandomStream random, float roomHeight = float.PositiveInfinity, float minHeight = 0f,
                               Predicate<PropSizeVariant> allowed = null)
        {
            var sizes = new List<PropSizeVariant> { new(gameObject, _ownWeight) };
            foreach (PropSizeVariant variant in _variants)
                if (variant.Prefab != null && roomHeight >= variant.MinRoomHeight
                    && (minHeight <= 0f || PropHeights.Of(variant.Prefab) >= minHeight)
                    && (allowed == null || allowed(variant)))
                    sizes.Add(variant);

            GameObject picked = random.PickWeighted(sizes, size => size.Weight).Prefab;
            return picked != null ? picked : gameObject;
        }
    }
}
