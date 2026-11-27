using UnityEngine;

namespace CGD.Level
{
    // Where a door, gate or shutter stands in a doorway (LevelDoorway): on the floor in the middle
    // of the opening, +Z out of the room, in world space, and how many tiles wide the opening is
    // (√2 across a 45° wall). The prefabs are made for a one-tile doorway, so they are stretched
    // across to fit.
    public readonly struct DoorwaySpot
    {
        public DoorwaySpot(Pose pose, float width)
        {
            Pose  = pose;
            Width = width;
        }

        public Pose  Pose  { get; }
        public float Width { get; }

        // `prefab` stood here, `inset` metres back into the room, stretched across the opening.
        public GameObject Place(GameObject prefab, Transform parent, float inset = 0f)
        {
            GameObject placed = Object.Instantiate(prefab, Pose.position - Pose.forward * inset, Pose.rotation, parent);
            if (!Mathf.Approximately(Width, 1f))
                placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(Width, 1f, 1f));
            return placed;
        }
    }
}
