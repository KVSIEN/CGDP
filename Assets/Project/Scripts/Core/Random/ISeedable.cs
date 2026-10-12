using UnityEngine;

namespace CGD.Core
{
    // Implemented by components that roll something of their own at runtime (a chest's loot,
    // an enemy's affixes, a vendor's shelf). Whatever spawns or places them can hand them a
    // seed, so the same seed rolls the same things however the player gets to them. Without
    // one they keep rolling at random.
    public interface ISeedable
    {
        void ApplySeed(Seed seed);

        // Seeds every ISeedable on `root` and below, each from its own child of `seed` (by its
        // order in the hierarchy), so two loot droppers on one prefab don't roll alike.
        static void SeedAll(GameObject root, Seed seed)
        {
            if (root == null) return;

            ISeedable[] seedables = root.GetComponentsInChildren<ISeedable>(true);
            for (int i = 0; i < seedables.Length; i++)
                seedables[i].ApplySeed(seed.Derive(i));
        }
    }
}
