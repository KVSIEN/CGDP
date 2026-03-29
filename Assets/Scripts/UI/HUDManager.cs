using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CrosshairHUD _crosshair;
    [SerializeField] private StatsHUD     _stats;
    [SerializeField] private AbilityHUD   _abilities;

    public CrosshairHUD Crosshair  => _crosshair;
    public StatsHUD     Stats      => _stats;
    public AbilityHUD   Abilities  => _abilities;

    public void ShowAll()
    {
        _crosshair?.Show();
        _stats?.Show();
        _abilities?.Show();
    }

    public void HideAll()
    {
        _crosshair?.Hide();
        _stats?.Hide();
        _abilities?.Hide();
    }

    public void RefreshAll()
    {
        _crosshair?.Refresh();
        _stats?.Refresh();
        _abilities?.Refresh();
    }
}
