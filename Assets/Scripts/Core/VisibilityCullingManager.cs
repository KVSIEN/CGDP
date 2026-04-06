using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Disables renderers on objects outside the camera frustum to reduce draw calls.
/// Checks are spread across multiple frames to avoid per-frame spikes.
///
/// Hysteresis prevents pop-in: currently-invisible objects use expanded bounds for
/// their visibility test, so they activate slightly before fully entering the frustum.
/// </summary>
[DefaultExecutionOrder(-100)]
public class VisibilityCullingManager : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [Tooltip("World-unit expansion added to an invisible object's bounds — activates it before it fully enters the frustum.")]
    [SerializeField] private float _activationMargin = 3f;

    [Tooltip("World-unit expansion added to a visible object's bounds — keeps it active until it is this far outside the frustum. " +
             "Must be less than Activation Margin to form a proper hysteresis gap.")]
    [SerializeField] private float _deactivationMargin = 5f;

    [Tooltip("Objects whose bounds centre is within this distance of the camera are always rendered.")]
    [SerializeField] private float _alwaysVisibleDistance = 12f;

    [Tooltip("Spread the full object list across this many frames (lower = more responsive, higher = cheaper per frame).")]
    [SerializeField] private int _batchFrames = 4;

    private static VisibilityCullingManager _instance;

    private readonly List<CullableObject> _objects = new();
    private readonly Plane[] _frustumPlanes = new Plane[6];
    private int _batchIndex;

    private void Awake() => _instance = this;

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void Register(CullableObject obj)
    {
        if (_instance == null || _instance._objects.Contains(obj)) return;
        _instance._objects.Add(obj);
    }

    public static void Unregister(CullableObject obj) => _instance?._objects.Remove(obj);

    private void LateUpdate()
    {
        int count = _objects.Count;
        if (_camera == null || count == 0) return;

        GeometryUtility.CalculateFrustumPlanes(_camera, _frustumPlanes);

        Vector3 camPos = _camera.transform.position;
        float minDistSq = _alwaysVisibleDistance * _alwaysVisibleDistance;

        int batchSize = Mathf.Max(1, Mathf.CeilToInt((float)count / _batchFrames));
        int start = _batchIndex * batchSize;
        int end = Mathf.Min(start + batchSize, count);

        for (int i = start; i < end; i++)
        {
            CullableObject obj = _objects[i];
            if (obj == null) continue;

            obj.RefreshBounds();
            Bounds bounds = obj.WorldBounds;

            // Never cull objects close to the camera.
            if ((bounds.center - camPos).sqrMagnitude <= minDistSq)
            {
                obj.SetVisible(true);
                continue;
            }

            // Hysteresis: expand the test bounds only when the object is currently invisible.
            // This means an invisible object starts rendering a little before it reaches the
            // actual screen edge, so the player never sees it pop in.
            // Hysteresis: invisible objects use a larger expansion (activate before entering view),
            // visible objects use a smaller expansion (stay active until clearly off-screen).
            // The gap between the two margins is the dead zone where no toggling occurs.
            Bounds testBounds = bounds;
            testBounds.Expand(obj.IsVisible ? _deactivationMargin * 2f : _activationMargin * 2f);

            obj.SetVisible(GeometryUtility.TestPlanesAABB(_frustumPlanes, testBounds));
        }

        _batchIndex = (_batchIndex + 1) % _batchFrames;

        // Clean up entries for destroyed objects once per full cycle.
        if (_batchIndex == 0)
            _objects.RemoveAll(o => o == null);
    }
}
