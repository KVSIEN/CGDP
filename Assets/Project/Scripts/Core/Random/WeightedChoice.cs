namespace CGD.Core
{
    // One item drawn in proportion to its weight from a run of candidates offered one at a time,
    // in a single pass and without a list (each offer may replace the pick so far with
    // probability weight / total so far, which leaves every candidate its fair share). Items
    // weighing 0 or less are never picked.
    // Unseeded (UnityEngine.Random): for live behaviour, not generation. Seeded picks go
    // through RandomStream, so a seed keeps giving the same result.
    public struct WeightedChoice<T>
    {
        private float _total;

        public T    Picked  { get; private set; }
        public bool HasPick { get; private set; }

        public void Offer(T item, float weight)
        {
            if (weight <= 0f) return;

            _total += weight;
            if (UnityEngine.Random.value * _total >= weight) return;

            Picked  = item;
            HasPick = true;
        }
    }
}
