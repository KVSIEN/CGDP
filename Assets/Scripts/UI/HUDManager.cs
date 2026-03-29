using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CrosshairHUD _crosshair;
    [SerializeField] private StatsHUD     _stats;
    [SerializeField] private AbilityHUD   _abilities;
    [SerializeField] private DodgeHUD     _dodge;

    public CrosshairHUD Crosshair  => _crosshair;
    public StatsHUD     Stats      => _stats;
    public AbilityHUD   Abilities  => _abilities;
    public DodgeHUD     Dodge      => _dodge;

    public void ShowAll()
    {
        _crosshair?.Show();
        _stats?.Show();
        _abilities?.Show();
        _dodge?.Show();
    }

    public void HideAll()
    {
        _crosshair?.Hide();
        _stats?.Hide();
        _abilities?.Hide();
        _dodge?.Hide();
    }

    public void RefreshAll()
    {
        _crosshair?.Refresh();
        _stats?.Refresh();
        _abilities?.Refresh();
        _dodge?.Refresh();
    }
}
