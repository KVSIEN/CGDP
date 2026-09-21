using System.Collections.Generic;
using UnityEngine;

namespace CGD.UI
{
    // Spawns and arranges every damage number in the game. Creates itself on the first hit
    // and lives under DontDestroyOnLoad, so nothing needs placing in a scene.
    //
    // Each hit gets its own number — a ten-pellet blast shows ten — and hits landing close
    // together are laid out as one group: each takes the next slot in a fixed alternating
    // pattern around the impact and is thrown upward from there. The group stays deliberately
    // tight and numbers overlap where they cross; nothing pushes them apart, so the pattern
    // alone decides the shape a burst builds. Offsets are in screen pixels, so spacing reads the
    // same at any range, and each number is placed back in the world at the depth of its own
    // impact to keep it sitting on the thing it came from.
    public class DamageNumbers : MonoBehaviour
    {
        private const float MinDepth = 0.2f; // nearer than this, the anchor is level with or behind the camera

        private static DamageNumbers _instance;

        private readonly Stack<DamagePopup> _pool   = new();
        private readonly List<DamagePopup>  _active = new();

        private int _uiLayer;
        private int _sortingOrder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static void Spawn(float damage, Vector3 worldPosition, bool critical)
        {
            if (damage <= 0f) return;

            DamageNumbers numbers = GetOrCreate();
            if (numbers != null) numbers.Add(damage, worldPosition, critical);
        }

        private static DamageNumbers GetOrCreate()
        {
            if (_instance != null) return _instance;
            if (UIOverlayCamera.GetOrCreate() == null) return null;

            var go = new GameObject("DamageNumbers");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DamageNumbers>();
            return _instance;
        }

        private void Awake() => _uiLayer = LayerMask.NameToLayer("UI");

        private void Add(float damage, Vector3 worldPosition, bool critical)
        {
            int stackDepth = CountCluster(worldPosition);
            MakeRoom();

            DamagePopup popup = Rent();

            if (++_sortingOrder > 10000) _sortingOrder = 1;
            popup.Show(damage, worldPosition, critical, stackDepth, _sortingOrder);
            _active.Add(popup);
        }

        // Numbers already sitting on this impact both decide the slot the new one takes in the
        // group and take a bump from it, which is what makes a burst read as damage piling up.
        private int CountCluster(Vector3 worldPosition)
        {
            float radiusSqr = DamageNumberStyle.ClusterRadius * DamageNumberStyle.ClusterRadius;
            int   count     = 0;

            for (int i = 0; i < _active.Count; i++)
            {
                if ((_active[i].Anchor - worldPosition).sqrMagnitude > radiusSqr) continue;
                _active[i].Kick();
                count++;
            }
            return count;
        }

        // Clears space for one more number. Approaching the cap the oldest is told to fade, which
        // under ordinary spam is all it takes; at the cap it is recycled outright, because a fade
        // takes time a held trigger will not give.
        private void MakeRoom()
        {
            if (_active.Count < DamageNumberStyle.MaxActive - DamageNumberStyle.RetireHeadroom) return;

            int oldest = OldestIndex();
            if (_active.Count < DamageNumberStyle.MaxActive)
            {
                _active[oldest].Retire();
                return;
            }

            Release(_active[oldest]);
            _active.RemoveAt(oldest);
        }

        private int OldestIndex()
        {
            int oldest = 0;
            for (int i = 1; i < _active.Count; i++)
            {
                if (_active[i].Age > _active[oldest].Age) oldest = i;
            }
            return oldest;
        }

        // Runs after gameplay has moved the camera, so numbers never lag a frame behind it.
        private void LateUpdate()
        {
            if (_active.Count == 0) return;

            var camera = Camera.main;
            if (camera == null)
            {
                ReleaseAll();
                return;
            }

            Advance(Time.deltaTime);
            Place(camera);
        }

        private void Advance(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Tick(deltaTime)) continue;
                Release(_active[i]);
                _active.RemoveAt(i);
            }
        }

        // Puts every number on screen: its own offset from its impact, at its impact's depth.
        // Numbers behind the camera are hidden rather than released — they are back on screen
        // the moment the player turns around.
        private void Place(Camera camera)
        {
            Transform camTransform = camera.transform;
            Vector3   camPosition  = camTransform.position;
            Vector3   camForward   = camTransform.forward;

            // One screen pixel covers this many world units per metre of depth. Scaling by the
            // depth of each number is what keeps every number the same size on screen.
            float unitsPerPixelPerMetre =
                Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f / Screen.height;

            for (int i = 0; i < _active.Count; i++)
            {
                DamagePopup popup = _active[i];
                float       depth = Vector3.Dot(popup.Anchor - camPosition, camForward);
                bool        ahead = depth > MinDepth;

                popup.SetVisible(ahead);
                if (!ahead) continue;

                Vector3 screen = camera.WorldToScreenPoint(popup.Anchor);

                popup.Place(camera, new Vector2(screen.x, screen.y) + popup.Offset,
                            depth, depth * unitsPerPixelPerMetre);
            }
        }

        private DamagePopup Rent()
        {
            while (_pool.Count > 0)
            {
                DamagePopup pooled = _pool.Pop();
                if (pooled != null) return pooled;
            }
            return Create();
        }

        private DamagePopup Create()
        {
            var go = new GameObject("DamagePopup");
            go.layer = _uiLayer;
            go.transform.SetParent(transform, false);

            var popup = go.AddComponent<DamagePopup>();
            popup.Build();
            return popup;
        }

        private void Release(DamagePopup popup)
        {
            popup.gameObject.SetActive(false);
            _pool.Push(popup);
        }

        private void ReleaseAll()
        {
            for (int i = 0; i < _active.Count; i++)
                Release(_active[i]);
            _active.Clear();
        }
    }
}
