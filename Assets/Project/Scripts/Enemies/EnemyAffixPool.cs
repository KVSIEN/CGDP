using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // The affixes enemies roll from, and how many each tier gets (0 at tier 1, more higher up).
    [CreateAssetMenu(fileName = "EnemyAffixPool", menuName = "CGD/Enemies/Enemy Affix Pool")]
    public class EnemyAffixPool : ScriptableObject
    {
        [SerializeField] private EnemyAffix[] _affixes = Array.Empty<EnemyAffix>();
        [Tooltip("Affixes rolled per tier: element 0 = tier 1, 1 = tier 2, 2 = tier 3")]
        [SerializeField] private int[] _countPerTier = { 0, 1, 2 };

        public int CountFor(int tier)
        {
            if (_countPerTier.Length == 0) return 0;
            int index = Mathf.Clamp(tier - 1, 0, _countPerTier.Length - 1);
            return Mathf.Max(0, _countPerTier[index]);
        }

        // `count` different affixes, picked at random.
        public void Pick(int count, List<EnemyAffix> into)
        {
            into.Clear();
            var pool = new List<EnemyAffix>(_affixes.Length);
            foreach (EnemyAffix affix in _affixes)
                if (affix != null && !pool.Contains(affix)) pool.Add(affix);

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                into.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
        }
    }
}
