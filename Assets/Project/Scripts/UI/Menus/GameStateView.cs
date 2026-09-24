using System;
using UnityEngine;
using CGD.Flow;

namespace CGD.UI
{
    // Shows a UI object only in the listed game states: a pause panel in Paused, a game-over
    // screen in GameOver, a loading screen in Loading and LevelTransition. Target must be
    // a different object from this one, so the view keeps listening while it is hidden.
    public class GameStateView : MonoBehaviour
    {
        [SerializeField] private GameObject  _target;
        [SerializeField] private GameState[] _visibleIn = Array.Empty<GameState>();

        private GameFlow _flow;

        private void OnEnable()
        {
            _flow = GameFlow.Instance;
            if (_flow == null) return;

            _flow.StateChanged += OnStateChanged;
            Apply(_flow.State);
        }

        private void OnDisable()
        {
            if (_flow != null) _flow.StateChanged -= OnStateChanged;
            _flow = null;
        }

        private void OnStateChanged(GameState previous, GameState next) => Apply(next);

        private void Apply(GameState state)
        {
            if (_target != null) _target.SetActive(Array.IndexOf(_visibleIn, state) >= 0);
        }
    }
}
