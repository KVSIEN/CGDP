using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // One step of a room function's structure: pillars, dividers… Runs after doorways,
    // walkways and the centre spot are tagged, so rules can keep those clear.
    public abstract class RoomStructureRule : ScriptableObject
    {
        public abstract void Apply(RoomStructure structure, RandomStream random);
    }
}
