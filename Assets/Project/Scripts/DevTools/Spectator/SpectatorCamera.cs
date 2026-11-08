using UnityEngine;
using CGD.Enemies;

namespace CGD.DevTools
{
    // A fixed overview for watching AI with the player switched off. Its camera (tagged
    // MainCamera, so world-space health bars and awareness marks face it) and audio listener
    // run only while the gameplay camera is off — deactivate the Player and it takes over,
    // turn the Player back on and it steps aside. While it runs, every agent under _agents
    // gets an on-screen label (AgentLabels) at a fixed text size, readable from up here.
    public class SpectatorCamera : MonoBehaviour
    {
        [SerializeField] private Camera        _camera;
        [SerializeField] private AudioListener _listener;
        [Tooltip("The Player's camera: the spectator view runs only while this one is off")]
        [SerializeField] private Camera        _gameplayCamera;
        [Tooltip("Every EnemyAI under this gets a label")]
        [SerializeField] private Transform     _agents;

        [Header("Labels")]
        [Tooltip("Metres above an agent's feet the label sits at")]
        [SerializeField, Min(0f)] private float   _labelHeight = 3f;
        [Tooltip("Label box, in pixels at 1920 × 1080")]
        [SerializeField] private Vector2 _labelSize = new(250f, 66f);
        [SerializeField, Min(6f)] private float   _fontSize = 15f;

        private AgentLabels _labels;
        private bool        _spectating;

        private void Awake()
        {
            EnemyAI[] agents = _agents != null ? _agents.GetComponentsInChildren<EnemyAI>(true) : System.Array.Empty<EnemyAI>();
            _labels = new AgentLabels(transform, agents, _labelSize, _fontSize);
            Apply(ShouldSpectate);
        }

        // Late, so labels follow agents after they moved this frame.
        private void LateUpdate()
        {
            bool spectate = ShouldSpectate;
            if (spectate != _spectating) Apply(spectate);
            if (_spectating) _labels.Tick(_camera, _labelHeight, Time.unscaledTime);
        }

        private bool ShouldSpectate => _gameplayCamera == null || !_gameplayCamera.isActiveAndEnabled;

        private void Apply(bool spectate)
        {
            _spectating = spectate;
            if (_camera != null) _camera.enabled = spectate;
            if (_listener != null) _listener.enabled = spectate;
            _labels.SetVisible(spectate);
        }
    }
}
