using System.Collections.Generic;
using CGD.Core;

namespace CGD.Algorithms
{
    // A shuffle bag: random picks that are fair over time. Like drawing tiles from a bag without
    // putting them back: every item comes out once (or as many times as it was added) before the
    // bag refills and anything repeats.
    //
    // How it works: the items sit in a list with a count of how many are still "in the bag" (the
    // first `remaining` items). A draw picks a random one of those, swaps it to the end of the
    // remaining part so it is out of the bag, and returns it. When none remain, all go back in.
    //
    // Why it's useful: plain random picks feel unfair. A 1-in-4 drop can miss ten times running,
    // and the same voice line can play three times in a row. A bag guarantees the spread while
    // staying unpredictable: barks and voice lines, loot that is "random but you'll get it
    // eventually", which prop variant goes next, spawn points, music tracks, a tetromino sequence.
    // Add an item several times to make it come up more often per round.
    public sealed class ShuffleBag<T>
    {
        private readonly List<T> _items = new();
        private readonly RandomStream _random;
        private int _remaining;

        public ShuffleBag(RandomStream random) => _random = random;

        public int Count => _items.Count;

        // Adds `copies` of the item, all in the bag now.
        public void Add(T item, int copies = 1)
        {
            for (int i = 0; i < copies; i++)
            {
                _items.Add(item);
                // Moved into the part still in the bag, so it can come out this round.
                Swap(_items.Count - 1, _remaining);
                _remaining++;
            }
        }

        // The next item. The bag must have something in it.
        public T Next()
        {
            if (_remaining == 0) _remaining = _items.Count;   // all back in the bag

            int pick = _random.Range(0, _remaining);
            T item = _items[pick];
            Swap(pick, _remaining - 1);                         // out of the bag
            _remaining--;
            return item;
        }

        private void Swap(int a, int b) => (_items[a], _items[b]) = (_items[b], _items[a]);
    }
}
