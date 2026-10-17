using CGD.Core;

namespace CGD.Level
{
    // The Puzzle room's calibration: a ring of nodes, each on or off. Using a node flips it
    // and both its neighbours; the puzzle is solved when every node is on. It starts from a
    // solved ring scrambled by random presses, so it can always be solved (pressing the
    // same nodes again undoes them).
    public class LightsOutPuzzle
    {
        private readonly bool[] _on;

        public LightsOutPuzzle(int count)
        {
            _on = new bool[System.Math.Max(3, count)];
            for (int i = 0; i < _on.Length; i++) _on[i] = true;
        }

        public int  Count => _on.Length;
        public bool IsSolved
        {
            get
            {
                foreach (bool on in _on)
                    if (!on) return false;
                return true;
            }
        }

        public bool IsOn(int index) => _on[index];

        public void Press(int index)
        {
            int n = _on.Length;
            Flip((index + n - 1) % n);
            Flip(index);
            Flip((index + 1) % n);
        }

        // Presses `presses` distinct random nodes, retrying until the ring isn't solved.
        public void Scramble(RandomStream random, int presses)
        {
            do
            {
                var picked = new bool[_on.Length];
                for (int i = 0; i < presses; i++)
                {
                    int node = random.Range(0, _on.Length);
                    picked[node] = !picked[node];
                }
                for (int i = 0; i < picked.Length; i++)
                    if (picked[i]) Press(i);
            }
            while (IsSolved);
        }

        // The other nodes a press on `index` also flips, for prompts.
        public (int left, int right) Neighbours(int index) =>
            ((index + _on.Length - 1) % _on.Length, (index + 1) % _on.Length);

        private void Flip(int index) => _on[index] = !_on[index];
    }
}
