using System.Collections.Generic;

namespace CGD.Algorithms
{
    // A min-heap (priority queue): put items in with a priority, always take out the one with the
    // lowest priority first.
    //
    // How it works: the items sit in a list that is read as a binary tree (item i's children are
    // at 2i + 1 and 2i + 2) with one rule: no item has a lower priority than its parent, so the
    // lowest is always at the top (index 0). Push adds an item at the end and swaps it up past
    // any parent with a higher priority; Pop takes the top, moves the last item into its place
    // and swaps it down past any child with a lower priority. Both take about log₂(n) swaps, so
    // a heap of a million items needs about 20.
    //
    // Why it's useful: whenever you repeatedly need "the cheapest / nearest / soonest next":
    // A* and Dijkstra pathfinding (the next tile to explore), a scheduler (the next event due),
    // growing something outward in order of distance. A sorted list would need re-sorting on
    // every insert. (.NET's PriorityQueue isn't available in Unity's .NET Standard 2.1.)
    //
    // Items with equal priority come out in no particular order, but always the same order for
    // the same pushes, so seeded generation stays repeatable.
    public sealed class MinHeap<T>
    {
        private readonly List<(T item, float priority)> _items = new();

        public int Count => _items.Count;

        public void Clear() => _items.Clear();

        public void Push(T item, float priority)
        {
            _items.Add((item, priority));
            SiftUp(_items.Count - 1);
        }

        // The item with the lowest priority, removed. The heap must not be empty.
        public T Pop()
        {
            T top = _items[0].item;
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);
            SiftDown(0);
            return top;
        }

        public T Peek() => _items[0].item;

        // Pop, or false when the heap is empty.
        public bool TryPop(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }
            item = Pop();
            return true;
        }

        // Moves the item at `index` up until its parent's priority is no higher.
        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_items[parent].priority <= _items[index].priority) break;
                Swap(parent, index);
                index = parent;
            }
        }

        // Moves the item at `index` down until neither child has a lower priority.
        private void SiftDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1, right = left + 1, smallest = index;
                if (left  < _items.Count && _items[left].priority  < _items[smallest].priority) smallest = left;
                if (right < _items.Count && _items[right].priority < _items[smallest].priority) smallest = right;
                if (smallest == index) return;
                Swap(smallest, index);
                index = smallest;
            }
        }

        private void Swap(int a, int b) => (_items[a], _items[b]) = (_items[b], _items[a]);
    }
}
