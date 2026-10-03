using System;
using UnityEngine;
using CGD.Factions;

namespace CGD.Level
{
    // Which wall kit a faction's rooms wear (LevelBuildSettings), so a room reads as TECH,
    // BIO or VOID from its walls.
    [Serializable]
    public class FactionWallKit
    {
        public FactionDefinition Faction;
        public WallKit Kit;
    }
}
