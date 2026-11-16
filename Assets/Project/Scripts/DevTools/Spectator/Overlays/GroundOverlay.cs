using System.Collections.Generic;
using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // The ground the inspected agent has learned (EnemyGround): a square on every place it
    // knows, green for hidden, red for a vantage, orange for a tight spot, larger and brighter
    // the more familiar it is; a cross where something happened that still puts it off.
    internal static class GroundOverlay
    {
        private const float GroundLift = 0.08f;
        private static readonly List<GroundPlace> Places = new();

        public static void Draw(OverlayMesh mesh, EnemyInsight agent)
        {
            agent.CopyKnownPlaces(Places);
            foreach (GroundPlace place in Places)
            {
                Vector3 foot = place.Position + Vector3.up * GroundLift;
                if (place.Danger > 0.1f) mesh.Cross(foot, 0.6f, OverlayPalette.Faded(OverlayPalette.Threat, place.Danger), 2f);
                if (place.Traits == PlaceTraits.None) continue;

                float size = 0.25f + 0.6f * place.Familiarity;
                Square(mesh, foot, size, OverlayPalette.Faded(ColourOf(place.Traits), Mathf.Clamp01(0.25f + place.Familiarity)));
            }
        }

        // Squares, so learned places read apart from the belief's rings.
        private static void Square(OverlayMesh mesh, Vector3 centre, float half, Color colour)
        {
            Vector3 a = centre + new Vector3(-half, 0f, -half), b = centre + new Vector3(half, 0f, -half);
            Vector3 c = centre + new Vector3(half, 0f, half),   d = centre + new Vector3(-half, 0f, half);
            mesh.Line(a, b, colour, 1.5f);
            mesh.Line(b, c, colour, 1.5f);
            mesh.Line(c, d, colour, 1.5f);
            mesh.Line(d, a, colour, 1.5f);
        }

        private static Color ColourOf(PlaceTraits traits) =>
            (traits & PlaceTraits.Hidden)  != 0 ? OverlayPalette.Concealed :
            (traits & PlaceTraits.Vantage) != 0 ? OverlayPalette.Vantage :
            OverlayPalette.Tight;
    }
}
