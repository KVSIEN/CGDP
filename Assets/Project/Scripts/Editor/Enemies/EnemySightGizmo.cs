using UnityEditor;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Editor
{
    // Draws every enemy's view cone (EnemyData sight range and angle, AwarenessColors) on the ground in the
    // Scene view. In play mode it takes the colour of the enemy's awareness and draws a line
    // to the target it is currently sensing. Hide it with the Scene view's Gizmos menu.
    internal static class EnemySightGizmo
    {
        private const float GroundOffset = 0.05f;
        private const float TargetHeight = 1f;
        private const float FillAlpha    = 0.06f;
        private const float EdgeAlpha    = 0.6f;

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Draw(EnemyAI enemy, GizmoType gizmoType)
        {
            EnemyData data = enemy.Data;
            if (data == null) return;

            bool live = Application.isPlaying && enemy.Perception != null;
            if (live && enemy.State == EnemyAI.AiState.Dead) return;

            Color   color  = live ? AwarenessColors.Of(enemy.Awareness) : AwarenessColors.Unaware;
            Vector3 origin = enemy.transform.position + Vector3.up * GroundOffset;
            DrawCone(origin, enemy.transform.forward, data.SightRange, data.SightAngle, color,
                     (gizmoType & GizmoType.Selected) != 0);

            if (live && enemy.Perception.IsSensing)
            {
                Handles.color = color;
                Handles.DrawLine(origin + Vector3.up * TargetHeight,
                                 enemy.Perception.Target.transform.position + Vector3.up * TargetHeight, 2f);
            }
        }

        private static void DrawCone(Vector3 origin, Vector3 forward, float range, float angle, Color color, bool selected)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            Vector3 from = Quaternion.AngleAxis(-angle * 0.5f, Vector3.up) * forward.normalized;
            Vector3 to   = Quaternion.AngleAxis( angle * 0.5f, Vector3.up) * forward.normalized;

            // The selected enemy's cone stands out a little from the rest.
            float fill = selected ? FillAlpha * 2.5f : FillAlpha;
            Handles.color = new Color(color.r, color.g, color.b, fill);
            Handles.DrawSolidArc(origin, Vector3.up, from, angle, range);

            Handles.color = new Color(color.r, color.g, color.b, selected ? 1f : EdgeAlpha);
            Handles.DrawWireArc(origin, Vector3.up, from, angle, range);
            Handles.DrawLine(origin, origin + from * range);
            Handles.DrawLine(origin, origin + to * range);
        }
    }
}
