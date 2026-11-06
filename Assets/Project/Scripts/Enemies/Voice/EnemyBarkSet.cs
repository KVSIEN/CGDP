using System;
using UnityEngine;

namespace CGD.Enemies
{
    // An enemy type's voice: which lines it says for each BarkKind (EnemyData › Voice). One
    // per faction or archetype — Tech bots chirp, Bio creatures snarl. Kinds without a line
    // stay silent, so a set can start small.
    [CreateAssetMenu(fileName = "NewEnemyBarkSet", menuName = "CGD/Enemies/Bark Set")]
    public class EnemyBarkSet : ScriptableObject
    {
        [SerializeField] private BarkLine[] _lines = Array.Empty<BarkLine>();

        // Lines by kind, built on first use (each bark is a lookup, not a search).
        [NonSerialized] private BarkLine[] _byKind;

        public bool TryGet(BarkKind kind, out BarkLine line)
        {
            _byKind ??= Index(_lines);
            line = _byKind[(int)kind];
            return line != null && line.Sound != null;
        }

        private static BarkLine[] Index(BarkLine[] lines)
        {
            var byKind = new BarkLine[Enum.GetValues(typeof(BarkKind)).Length];
            foreach (BarkLine line in lines)
                if (line != null && line.Kind != BarkKind.None) byKind[(int)line.Kind] ??= line;
            return byKind;
        }

        private void OnValidate() => _byKind = null;
    }
}
