using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CrosshairHUD  _crosshair;
    [SerializeField] private StatsHUD      _stats;
    [SerializeField] private AmmoHUD       _ammo;
    [SerializeField] private AbilityHUD    _abilities;
    [SerializeField] private DodgeHUD      _dodge;
    [SerializeField] private HitEffect     _hitEffect;
    [SerializeField] private VelocityHUD   _velocity;
    [SerializeField] private InventoryHUD  _inventory;
    [SerializeField] private InteractHUD   _interact;

    public CrosshairHUD  Crosshair  => _crosshair;
    public StatsHUD      Stats      => _stats;
    public AmmoHUD       Ammo       => _ammo;
    public AbilityHUD    Abilities  => _abilities;
    public DodgeHUD      Dodge      => _dodge;
    public HitEffect     HitEffect  => _hitEffect;
    public VelocityHUD   Velocity   => _velocity;
    public InventoryHUD  Inventory  => _inventory;
    public InteractHUD   Interact   => _interact;

    public void ShowAll()
    {
        _crosshair?.Show();
        _stats?.Show();
        _ammo?.Show();
        _abilities?.Show();
        _dodge?.Show();
        _hitEffect?.Show();
        _velocity?.Show();
        _interact?.Show();
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
        _inventory?.Hide();
        _interact?.Hide();
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
        _inventory?.Refresh();
        _interact?.Refresh();
    }
}
