using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CrosshairHUD  _crosshair;
    [SerializeField] private StatsHUD      _stats;
    [SerializeField] private WeaponHUD     _weapon;
    [SerializeField] private AbilityHUD    _abilities;
    [SerializeField] private DodgeHUD      _dodge;
    [SerializeField] private HitEffect     _hitEffect;
    [SerializeField] private VelocityHUD   _velocity;
    [SerializeField] private InventoryHUD  _inventory;
    [SerializeField] private InteractHUD   _interact;

    public CrosshairHUD  Crosshair  => _crosshair;
    public StatsHUD      Stats      => _stats;
    public WeaponHUD     Weapon     => _weapon;
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
        _weapon?.Show();
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
        _weapon?.Hide();
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
        _weapon?.Refresh();
        _abilities?.Refresh();
        _dodge?.Refresh();
        _hitEffect?.Refresh();
        _velocity?.Refresh();
        _inventory?.Refresh();
        _interact?.Refresh();
    }
}
