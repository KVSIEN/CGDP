using System.Collections.Generic;
using System.Globalization;

namespace CGD.Core
{
    // Seeds counting up from a typed one, each still a seed you can type (for sweeps such as
    // the map style report): a number counts up ("100" → 100, 101…), text ending in a number
    // counts that number up ("run 7" → "run 7", "run 8"…, keeping leading zeros), and other
    // text gets a counter ("banana" → "banana 1", "banana 2"…).
    public static class SeedSequence
    {
        public static IEnumerable<string> CountingFrom(string first, int count)
        {
            first = first?.Trim() ?? string.Empty;

            if (long.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number))
            {
                for (int i = 0; i < count; i++) yield return (number + i).ToString(CultureInfo.InvariantCulture);
                yield break;
            }

            int digitsAt = first.Length;
            while (digitsAt > 0 && char.IsDigit(first[digitsAt - 1])) digitsAt--;

            string prefix = first.Substring(0, digitsAt);
            string digits = first.Substring(digitsAt);
            if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out long start))
            {
                // No number to count up (or one too long): add a counter instead.
                prefix = first.Length > 0 ? first + " " : string.Empty;
                digits = string.Empty;
                start  = 1;
            }

            for (int i = 0; i < count; i++)
                yield return prefix + (start + i).ToString(CultureInfo.InvariantCulture).PadLeft(digits.Length, '0');
        }
    }
}
