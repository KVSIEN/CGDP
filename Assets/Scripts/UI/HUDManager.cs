using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CrosshairHUD _crosshair;
    [SerializeField] private StatsHUD     _stats;
    [SerializeField] private AmmoHUD      _ammo;
    [SerializeField] private AbilityHUD   _abilities;
    [SerializeField] private DodgeHUD     _dodge;
    [SerializeField] private HitEffect    _hitEffect;
    [SerializeField] private VelocityHUD  _velocity;

    public CrosshairHUD Crosshair  => _crosshair;
    public StatsHUD     Stats      => _stats;
    public AmmoHUD      Ammo       => _ammo;
    public AbilityHUD   Abilities  => _abilities;
    public DodgeHUD     Dodge      => _dodge;
    public HitEffect    HitEffect  => _hitEffect;
    public VelocityHUD  Velocity   => _velocity;

    public void ShowAll()
    {
        _crosshair?.Show();
        _stats?.Show();
        _ammo?.Show();
        _abilities?.Show();
        _dodge?.Show();
        _hitEffect?.Show();
        _velocity?.Show();
    }

    public void HideAll()
    {
        _crosshair?.Hide();
        _stats?.Hide();
        _ammo?.Hide();
        _abilities?.Hide();
        _dodge?.Hide();
        _hitEffect?.Hide();
        _velocity?.Hide();
    }

    public void RefreshAll()
    {
        _crosshair?.Refresh();
        _stats?.Refresh();
        _ammo?.Refresh();
        _abilities?.Refresh();
        _dodge?.Refresh();
        _hitEffect?.Refresh();
        _velocity?.Refresh();
    }
}
