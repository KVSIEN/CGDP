using System.Collections.Generic;
using UnityEngine;

namespace CGD.DevTools
{
    // A repeatable starting situation for the AI test arena, loaded from the spectator's
    // scenario list (B) and restarted with R: which agents take part, where they start, how
    // hurt they are, what they already know — so a branch built for "Void with its shield
    // broken" or "Bio badly hurt in Tech's sight" can be watched again and again.
    [CreateAssetMenu(fileName = "AIScenario", menuName = "CGD/DevTools/AI Scenario")]
    public class AIScenario : ScriptableObject
    {
        public string DisplayName = "Scenario";
        [TextArea(1, 3)] public string Description;

        public List<ScenarioAgent> Agents = new();
        [Tooltip("Agents not listed sit out (switched off). Off = they start over at home, untouched by the scenario")]
        public bool OthersSitOut = true;

        [Tooltip("Start the match stats over when it loads")]
        public bool ResetStats = true;
        [Tooltip("Pause the game once it is set up, to look around before it starts")]
        public bool StartPaused;
        [Tooltip("Name of the agent the camera watches once it loads. Empty = the camera stays as it is")]
        public string Watch;
    }
}
