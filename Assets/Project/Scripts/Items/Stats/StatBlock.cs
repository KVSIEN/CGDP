using UnityEngine;

namespace CGD.Items
{
    // A dense stat -> value map. Backed by a flat array indexed by the enum, so
    // reads are allocation-free and safe to do during resolution every frame if a
    // consumer ever needs to.
    public sealed class StatBlock
    {
        private readonly float[] _values = new float[ItemStatTraits.Count];

        public float this[ItemStat stat]
        {
            get => _values[(int)stat];
            set => _values[(int)stat] = value;
        }

        public void Add(ItemStat stat, float amount) => _values[(int)stat] += amount;

        public void Clear() => System.Array.Clear(_values, 0, _values.Length);

        public bool Has(ItemStat stat) => !Mathf.Approximately(_values[(int)stat], 0f);
    }
}
