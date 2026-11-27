using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // A structure rule that can build in one part of a room: the structure budget points it at
    // the area that has least (StructureBudget).
    public interface IFocusedStructureRule
    {
        // As RoomStructureRule.Apply, but building only on tiles inside `focus`.
        void Apply(RoomStructure structure, RandomStream random, RectInt focus);
    }
}
