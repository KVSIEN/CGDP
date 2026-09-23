using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CGD.UI
{
    // Overlay camera stacked on main so world-space UI is never hidden by scene geometry.
    public static class UIOverlayCamera
    {
        private static Camera _camera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _camera = null;

        public static Camera GetOrCreate()
        {
            if (_camera != null) return _camera;

            var mainCam = Camera.main;
            if (mainCam == null) return null;

            int layer = LayerMask.NameToLayer("UI");

            // Parent to main camera so it always shares its transform
            var go = new GameObject("UIOverlayCamera");
            go.transform.SetParent(mainCam.transform, false);

            var cam = go.AddComponent<Camera>();
            cam.clearFlags  = CameraClearFlags.Depth;
            cam.cullingMask = 1 << layer;
            cam.depth       = mainCam.depth + 1;

            go.AddComponent<UIOverlayCameraSync>().Initialize(mainCam);

            var camData = go.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Overlay;

            // Add to main camera's URP stack
            var mainData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            mainData.cameraStack.Add(cam);

            // Exclude layer from main camera so it isn't rendered twice
            mainCam.cullingMask &= ~(1 << layer);

            _camera = cam;
            return cam;
        }
    }
}
