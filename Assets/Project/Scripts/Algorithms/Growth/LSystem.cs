using System.Collections.Generic;
using System.Text;

namespace CGD.Algorithms
{
    // An L-system (Lindenmayer system, Aristid Lindenmayer, 1968): grows a string of symbols by
    // rewriting every symbol by a rule, over and over. Read as drawing commands (Turtle2D), the
    // strings draw plants, trees, roots, coral and fractals.
    //
    // How it works: start from a short string (the axiom). Each round, replace every symbol that
    // has a rule with that rule's text, all at once; symbols with no rule are copied as they are.
    // Because a rule can contain its own symbol, the string branches out with every round, the
    // way a plant repeats its own structure at smaller and smaller scales.
    //
    // Example, a fern-like plant (angle 25°):
    //     axiom "X",  X → "F+[[X]-X]-F[-FX]+X",  F → "FF"
    // Round 1 turns "X" into "F+[[X]-X]-F[-FX]+X"; each round after that grows every X and F again.
    //
    // Why it's useful: compact, controllable growth. Vines creeping up a wall, roots breaking
    // through a floor, branching cracks, coral and fungus for the BIO reality, trees, lightning,
    // river deltas. Change one rule and the whole species changes. Keep rounds low: the string
    // roughly multiplies in length every round.
    public sealed class LSystem
    {
        private readonly Dictionary<char, string> _rules = new();

        public LSystem(string axiom) => Axiom = axiom;

        public string Axiom { get; }

        // Every `symbol` is replaced by `replacement` each round.
        public LSystem AddRule(char symbol, string replacement)
        {
            _rules[symbol] = replacement;
            return this;
        }

        // The string after `rounds` rounds of rewriting (0 = the axiom).
        public string Generate(int rounds)
        {
            string current = Axiom;
            for (int round = 0; round < rounds; round++)
            {
                var next = new StringBuilder(current.Length * 2);
                foreach (char symbol in current)
                    next.Append(_rules.TryGetValue(symbol, out string replacement) ? replacement : symbol.ToString());
                current = next.ToString();
            }
            return current;
        }
    }
}
