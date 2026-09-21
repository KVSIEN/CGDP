using UnityEngine;

namespace CGD.UI
{
    // Keeps the UI overlay camera projecting exactly like the camera it is stacked on.
    // The overlay renders with its own projection, so a main camera that changes field of
    // view at runtime — aiming down sights, sprinting — would otherwise leave world-space UI
    // sized and placed against a projection it is no longer being drawn through.
    [RequireComponent(typeof(Camera))]
    public class UIOverlayCameraSync : MonoBehaviour
    {
        private Camera _overlay;
        private Camera _source;

        public void Initialize(Camera source)
        {
            _source = source;
            TryGetComponent(out _overlay);
            Sync();
        }

        private void LateUpdate()
        {
            if (_source == null || _overlay == null) return;
            Sync();
        }

        private void Sync()
        {
            _overlay.orthographic     = _source.orthographic;
            _overlay.orthographicSize = _source.orthographicSize;
            _overlay.fieldOfView      = _source.fieldOfView;
            _overlay.nearClipPlane    = _source.nearClipPlane;
            _overlay.farClipPlane     = _source.farClipPlane;
        }
    }
}
