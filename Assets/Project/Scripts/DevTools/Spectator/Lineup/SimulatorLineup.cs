using System.Collections.Generic;
using UnityEngine;

namespace CGD.DevTools
{
    // Who is in the AI Simulator when it starts playing: squads of enemies, each with its enemy
    // types and counts, a spawn spot and a patrol route. Edit it in the inspector (squads as
    // cards, spots as handles in the Scene view); the LineupSpawner in the scene builds it on
    // play. Keep several assets to switch between setups.
    [CreateAssetMenu(fileName = "SimulatorLineup", menuName = "CGD/DevTools/Simulator Lineup")]
    public class SimulatorLineup : ScriptableObject
    {
        public List<LineupSquad> Squads = new();
    }
}
