using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CGD.UI
{
    // Overlay camera stacked on main so world-space UI is never hidden by scene geometry.
    // Follows whichever camera is main: a scene can switch cameras (the AI test scenes'
    // spectator mode takes over when the Player is switched off), and an overlay left stacked
    // on a disabled camera would draw nothing.
    public static class UIOverlayCamera
    {
        private static Camera _camera;
        private static Camera _base;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _camera = null;
            _base   = null;
        }

        public static Camera GetOrCreate()
        {
            var mainCam = Camera.main;
            if (_camera != null && (mainCam == null || mainCam == _base)) return _camera;
            if (mainCam == null) return null;

            if (_camera == null) _camera = Create();
            Attach(mainCam);
            return _camera;
        }

        private static Camera Create()
        {
            var go = new GameObject("UIOverlayCamera");
            var cam = go.AddComponent<Camera>();
            cam.clearFlags  = CameraClearFlags.Depth;
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            go.AddComponent<UIOverlayCameraSync>();

            var camData = go.AddComponent<UniversalAdditionalCameraData>();
            camData.renderType = CameraRenderType.Overlay;
            return cam;
        }

        private static void Attach(Camera mainCam)
        {
            if (_base != null) _base.GetUniversalAdditionalCameraData().cameraStack.Remove(_camera);
            _base = mainCam;

            // Parent to main camera so it always shares its transform
            _camera.transform.SetParent(mainCam.transform, false);
            _camera.depth = mainCam.depth + 1;
            _camera.GetComponent<UIOverlayCameraSync>().Initialize(mainCam);

            // Added if the camera has none yet — URP doesn't add it to a camera on its own
            var mainData = mainCam.GetUniversalAdditionalCameraData();
            if (!mainData.cameraStack.Contains(_camera)) mainData.cameraStack.Add(_camera);

            // Exclude layer from main camera so it isn't rendered twice
            mainCam.cullingMask &= ~_camera.cullingMask;
        }
    }
}
