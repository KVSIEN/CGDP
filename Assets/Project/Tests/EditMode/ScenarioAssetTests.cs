using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using CGD.DevTools;

namespace CGD.Tests
{
    // The AI test scenarios hang together: every agent named in one (to watch, or to be aware
    // of) takes part in it, so nothing points at an agent that is sitting out.
    public class ScenarioAssetTests
    {
        [Test]
        public void ScenariosOnlyNameAgentsTakingPart()
        {
            string[] guids = AssetDatabase.FindAssets("t:AIScenario", new[] { "Assets/Project/Data" });
            Assert.IsNotEmpty(guids, "no AIScenario assets");

            foreach (string guid in guids)
            {
                var scenario = AssetDatabase.LoadAssetAtPath<AIScenario>(AssetDatabase.GUIDToAssetPath(guid));
                var taking = new HashSet<string>();
                foreach (ScenarioAgent agent in scenario.Agents)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(agent.Agent), $"{scenario.name}: an entry has no agent name");
                    Assert.IsTrue(taking.Add(agent.Agent.Trim().ToLowerInvariant()), $"{scenario.name}: {agent.Agent} listed twice");
                }

                foreach (ScenarioAgent agent in scenario.Agents)
                    if (!string.IsNullOrWhiteSpace(agent.AwareOf))
                        Assert.IsTrue(taking.Contains(agent.AwareOf.Trim().ToLowerInvariant()), $"{scenario.name}: {agent.Agent} is aware of {agent.AwareOf}, who isn't in it");
                if (!string.IsNullOrWhiteSpace(scenario.Watch) && scenario.OthersSitOut)
                    Assert.IsTrue(taking.Contains(scenario.Watch.Trim().ToLowerInvariant()), $"{scenario.name}: watches {scenario.Watch}, who sits out");
            }
        }
    }
}
