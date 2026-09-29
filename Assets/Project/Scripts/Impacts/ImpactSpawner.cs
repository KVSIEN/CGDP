using UnityEngine;
using CGD.Audio;
using CGD.Core;

namespace CGD.Impacts
{
    // Turns reported impacts into decals, pooled VFX and sounds, looked up per surface in an
    // ImpactDatabase. One per scene.
    public class ImpactSpawner : MonoBehaviour
    {
        [SerializeField] private ImpactDatabase _database;
        [Tooltip("Decals kept in the world at once; the oldest is reused after that")]
        [SerializeField, Min(0)] private int _maxDecals = 128;

        private DecalPool _decals;

        private void Awake() => _decals = new DecalPool(transform, _maxDecals);

        private void OnEnable()  => ImpactEvents.Occurred += OnImpact;
        private void OnDisable() => ImpactEvents.Occurred -= OnImpact;

        private void OnImpact(Impact impact)
        {
            if (_database == null) return;

            ImpactEffect effect = _database.Resolve(impact);
            if (effect == null) return;

            if (effect.Decal != null)
                _decals.Place(impact.Point, impact.Normal, impact.Collider, effect.Decal, effect.DecalSize.Evaluate());

            if (effect.Vfx != null)
                PrefabPool.Spawn(effect.Vfx, impact.Point, Quaternion.LookRotation(impact.Normal));

            effect.Sound.TryPlay(impact.Point);
        }
    }
}
