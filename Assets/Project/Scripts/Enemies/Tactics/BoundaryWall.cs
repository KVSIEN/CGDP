using UnityEngine;

namespace CGD.Enemies
{
    // Marks geometry that encloses the playable space (an arena's outer walls, a level's room
    // walls) as opposed to something standing in it. Enemies never count it as cover: pressed
    // against the edge of the world is a corner to be trapped in, not a place to fight from.
    // Crates, pillars and partition stubs are cover; leave those unmarked.
    [DisallowMultipleComponent]
    public class BoundaryWall : MonoBehaviour
    {
    }
}
