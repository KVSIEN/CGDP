using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Stops drawing the furniture of rooms the camera can't see into (RoomPortals): the level's
    // walls and floors are a few merged meshes and stay, but each room's props are separate
    // renderers, hundreds in a big level, and those in rooms behind walls are switched off
    // until a doorway toward them comes into view. Only how they are drawn changes; colliders,
    // lights and enemies are untouched. Added by LevelBuilder; disabling it shows everything,
    // so it can be compared on and off (/cull in the dev console) while profiling.
    public class RoomCuller : MonoBehaviour
    {
        private const float CheckEvery = 0.1f;
        // Rooms deep through doorways a room can still count as in view.
        private const int MaxDepth = 4;

        private readonly Dictionary<int, Renderer[]> _renderers = new();
        private readonly Dictionary<int, bool> _shown = new();
        private readonly HashSet<int> _visible = new();
        private readonly Plane[] _planes = new Plane[6];

        private LevelLayout _layout;
        private Transform _level;
        private Camera _camera;
        private Transform _player;
        private RoomPortals _portals;
        private System.Predicate<Bounds> _inView;
        private float _nextCheck;

        public int RoomsHidden { get; private set; }

        // `roomObjects`: what was placed in each room, by room id.
        public void Begin(LevelLayout layout, Transform level, Camera viewer, Transform player,
                          IReadOnlyDictionary<int, List<GameObject>> roomObjects)
        {
            _layout  = layout;
            _level   = level;
            _camera  = viewer;
            _player  = player;
            _portals = new RoomPortals(layout, ToWorld);
            _inView  = box => GeometryUtility.TestPlanesAABB(_planes, box);
            foreach (KeyValuePair<int, List<GameObject>> room in roomObjects)
            {
                var renderers = new List<Renderer>();
                foreach (GameObject placed in room.Value)
                    if (placed != null) renderers.AddRange(placed.GetComponentsInChildren<Renderer>(true));
                _renderers[room.Key] = renderers.ToArray();
                _shown[room.Key] = true;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _portals == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + CheckEvery;

            GeometryUtility.CalculateFrustumPlanes(_camera, _planes);
            if (!_portals.TryVisible(_level.InverseTransformPoint(_camera.transform.position), _inView, MaxDepth, _visible, PlayerRoom()))
            {
                ShowAll();
                return;
            }

            int hidden = 0;
            foreach (KeyValuePair<int, Renderer[]> room in _renderers)
            {
                bool show = _visible.Contains(room.Key);
                if (!show) hidden++;
                if (_shown[room.Key] != show) Show(room.Key, show);
            }
            RoomsHidden = hidden;
        }

        private void OnDisable() => ShowAll();

        private int PlayerRoom()
        {
            if (_player == null) return -1;
            LevelRoom room = _layout.RoomAtLocal(_level.InverseTransformPoint(_player.position));
            return room != null ? room.Node.Id : -1;
        }

        private void ShowAll()
        {
            foreach (int room in _renderers.Keys) if (!_shown[room]) Show(room, true);
            RoomsHidden = 0;
        }

        private void Show(int room, bool show)
        {
            _shown[room] = show;
            foreach (Renderer renderer in _renderers[room])
                if (renderer != null) renderer.forceRenderingOff = !show;
        }

        // A box in the level's space as the world-space box round it, whatever the level's turn.
        private Bounds ToWorld(Bounds local)
        {
            var world = new Bounds(_level.TransformPoint(local.center), Vector3.zero);
            Vector3 e = local.extents;
            for (int i = 0; i < 8; i++)
                world.Encapsulate(_level.TransformPoint(local.center + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
            return world;
        }
    }
}
