using System;
using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The spectator's world overlays, drawn into the Game view each frame for whichever are
    // switched on (SpectatorPreferences):
    //   senses         sight, hearing and targeting (SensesOverlay)
    //   cover          the cover spots and who holds them (CoverOverlay)
    //   paths & plans  where agents walk and their latest tactical searches (PlansOverlay)
    // With an agent inspected the overlays centre on it and show its details; without one
    // they show every agent more lightly, and cover around where the camera looks.
    internal sealed class SpectatorOverlays : IDisposable
    {
        private const float FocusDistance = 60f;

        private readonly AgentRoster   _agents;
        private readonly OverlayMesh  _mesh;
        private readonly CoverOverlay _cover = new();
        private readonly PlansOverlay _plans;

        public SpectatorOverlays(Material material, AgentRoster agents)
        {
            _agents = agents;
            _mesh   = new OverlayMesh(material);
            _plans  = new PlansOverlay(agents);
        }

        public static bool AnyOn(SpectatorPreferences preferences) =>
            preferences.ShowSenses || preferences.ShowCover || preferences.ShowPlans;

        public void Tick(Camera camera, SpectatorPreferences preferences, EnemyAI inspected, float now)
        {
            if (camera == null || !AnyOn(preferences)) return;

            _mesh.Begin(camera);
            EnemyInsight? focus = Alive(inspected) ? new EnemyInsight(inspected) : null;

            if (preferences.ShowCover) _cover.Draw(_mesh, focus, LookPoint(camera));
            if (preferences.ShowPlans) _plans.Refresh(now);

            for (int i = 0; i < _agents.Count; i++)
            {
                if (!Alive(_agents[i])) continue;
                bool detailed = _agents[i] == inspected;
                if (focus.HasValue && !detailed) continue;   // one agent at a time when inspecting

                var agent = new EnemyInsight(_agents[i]);
                if (preferences.ShowSenses) SensesOverlay.Draw(_mesh, agent, detailed || !focus.HasValue);
                if (preferences.ShowPlans)  _plans.DrawPath(_mesh, i, detailed);
            }

            if (preferences.ShowPlans && focus.HasValue) PlansOverlay.DrawSearches(_mesh, focus.Value, Time.time);
            if (preferences.ShowPlans && focus.HasValue) BeliefOverlay.Draw(_mesh, focus.Value);
            _mesh.Render();
        }

        // The HUD line saying which overlays are on and their keys.
        public static string Status(SpectatorPreferences preferences, SpectatorInputSettings settings) =>
            $"{Chip("Senses", preferences.ShowSenses, settings.PromptFor(SpectatorAction.ToggleSenses))}     " +
            $"{Chip("Cover", preferences.ShowCover, settings.PromptFor(SpectatorAction.ToggleCover))}     " +
            $"{Chip("Paths & plans", preferences.ShowPlans, settings.PromptFor(SpectatorAction.TogglePlans))}";

        public void Dispose() => _mesh.Dispose();

        private static string Chip(string name, bool on, string key) => on
            ? $"<color=#FFD24A><b>{key}</b></color> <b>{name}</b>"
            : $"<color=#6A7080>{key} {name}</color>";

        private static bool Alive(EnemyAI agent) =>
            agent != null && agent.gameObject.activeInHierarchy && agent.State != EnemyAI.AiState.Dead;

        // Where the view meets the ground (or a point ahead when looking up).
        private static Vector3 LookPoint(Camera camera)
        {
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) && distance < FocusDistance
                ? ray.GetPoint(distance)
                : ray.GetPoint(FocusDistance * 0.5f);
        }
    }
}
