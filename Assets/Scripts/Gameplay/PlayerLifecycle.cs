using System.Collections;
using UnityEngine;

public class PlayerLifecycle : MonoBehaviour
{
    [SerializeField] private PlayerStats         _stats;
    [SerializeField] private PlayerMovement      _movement;
    [SerializeField] private PlayerAbilities     _abilities;
    [SerializeField] private PlayerInputHandler  _input;
    [SerializeField] private WeaponController    _weapon;
    [SerializeField] private HUDManager          _hud;
    [SerializeField] private Transform           _spawnPoint;
    [SerializeField] private float               _respawnDelay = 3f;
    [SerializeField] private GameObject          _deathScreen;

    private void Awake()
    {
        _stats.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        _movement.enabled  = false;
        _abilities.enabled = false;
        _input.InputEnabled = false;
        _hud.HideAll();

        if (_deathScreen != null)
            _deathScreen.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

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
            transform.position = _spawnPoint.position;

        _stats.Respawn();
        _weapon?.Refill();

        _movement.enabled   = true;
        _abilities.enabled  = true;
        _input.InputEnabled = true;
        _hud.ShowAll();

        if (_deathScreen != null)
            _deathScreen.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
