using System;
using System.Collections.Generic;

namespace CGD.DevTools
{
    // The result of asking what the word at the end of a command line could become.
    public sealed class Completion
    {
        public Completion(string line, int tokenStart, IReadOnlyList<string> candidates, string signature)
        {
            Line       = line;
            TokenStart = tokenStart;
            Candidates = candidates;
            Signature  = signature;
        }

        public static Completion None(string line) => new(line, line?.Length ?? 0, Array.Empty<string>(), null);

        public string Line { get; }

        // Where the word being completed begins in Line.
        public int TokenStart { get; }

        public IReadOnlyList<string> Candidates { get; }

        // The usage of the command being typed, or null while the command isn't known yet.
        public string Signature { get; }

        public string Typed => Line.Substring(TokenStart);

        // Longest start shared by every candidate (the first candidate's casing).
        public string CommonPrefix
        {
            get
            {
                if (Candidates.Count == 0) return string.Empty;

                string prefix = Candidates[0];
                foreach (string candidate in Candidates)
                    while (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        prefix = prefix.Substring(0, prefix.Length - 1);
                return prefix;
            }
        }

        // The line with the word being completed replaced by a candidate.
        public string Apply(int index) => Line.Substring(0, TokenStart) + Candidates[index];
    }
}
