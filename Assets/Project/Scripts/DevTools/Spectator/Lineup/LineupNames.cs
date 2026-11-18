using System.Collections.Generic;

namespace CGD.DevTools
{
    // Names agents the way the scene's own are named: the first of a kind keeps the plain name
    // (Bio Agent), later ones are numbered from 2 (Bio Agent 2). Scenarios and labels find
    // agents by these names, so a lineup of the usual test agents works with the existing ones.
    public sealed class LineupNames
    {
        private readonly Dictionary<string, int> _issued = new();

        public string Next(string baseName)
        {
            _issued.TryGetValue(baseName, out int count);
            _issued[baseName] = ++count;
            return count == 1 ? baseName : $"{baseName} {count}";
        }
    }
}
