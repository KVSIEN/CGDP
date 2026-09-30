using UnityEngine;
using CGD.Core;

namespace CGD.Level
{
    // Columns evenly spaced on a circle around the room's middle — an atrium or dome
    // colonnade. Spots that land on a walkway or off the floor are left out.
    [CreateAssetMenu(fileName = "ColumnRingStructureRule", menuName = "CGD/Level/Structure/Column Ring")]
    public class RingPillarsStructureRule : RoomStructureRule
    {
        [SerializeField, Min(3)] private int _count = 8;
        [Tooltip("Circle radius as a fraction of half the room's smaller side")]
        [SerializeField, Range(0.2f, 0.95f)] private float _radius = 0.6f;
        [SerializeField, Min(0.1f)] private float _pillarWidth = 1f;
        [SerializeField] private GameObject _pillarPrefab;

        public override void Apply(RoomStructure structure, RandomStream random)
        {
            RectInt bounds = structure.Footprint.Bounds;
            float radius = Mathf.Min(bounds.width, bounds.height) * 0.5f * _radius;
            // Half a step off the axes, so no column stands where a doorway's walkway runs straight in.
            float offset = Mathf.PI / _count;

            for (int i = 0; i < _count; i++)
            {
                float angle = offset + i * 2f * Mathf.PI / _count;
                Vector2 position = bounds.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                structure.TryAddPillar(position, _pillarWidth, _pillarPrefab);
            }
        }
    }
}
