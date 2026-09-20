using System.Collections;
using UnityEngine;
using CGD.Abilities;
using CGD.Core;
using CGD.Input;
using CGD.UI;

namespace CGD.Player
{
    // Death and respawn flow: locks control and shows the death screen, then moves the
    // player to the spawn point and revives them. Per-system resets (ammo, cooldowns,
    // status effects) happen in those systems via PlayerHealth's OnDeath/OnRevived.
    public class PlayerLifecycle : MonoBehaviour
    {
        [SerializeField] private PlayerHealth        _health;
        [SerializeField] private PlayerMovement      _movement;
        [SerializeField] private PlayerAbilities     _abilities;
        [SerializeField] private PlayerInputHandler  _input;
        [SerializeField] private HUDManager          _hud;
        [SerializeField] private Transform           _spawnPoint;
        [SerializeField] private float               _respawnDelay = 3f;
        [SerializeField] private GameObject          _deathScreen;

        private void Awake()
        {
            _health.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            _health.OnDeath -= HandleDeath;
        }

        private void HandleDeath()
        {
            SetControlEnabled(false);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(_respawnDelay);
            Respawn();
        }

        private void Respawn()
        {
            if (_spawnPoint != null)
                _movement.Teleport(_spawnPoint.position);

            _health.Revive();
            SetControlEnabled(true);
        }

        private void SetControlEnabled(bool controlEnabled)
        {
            _movement.enabled   = controlEnabled;
            _abilities.enabled  = controlEnabled;
            _input.InputEnabled = controlEnabled;

            if (controlEnabled) _hud.ShowAll();
            else                _hud.HideAll();

            if (_deathScreen != null)
                _deathScreen.SetActive(!controlEnabled);

            CursorLock.Set(controlEnabled);
        }
    }
}
