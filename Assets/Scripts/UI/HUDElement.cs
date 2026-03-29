using UnityEngine;

public abstract class HUDElement : MonoBehaviour
{
    public bool IsVisible { get; private set; } = true;

    public void Show()
    {
        IsVisible = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        IsVisible = false;
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsVisible) Hide(); else Show();
    }

    public abstract void Refresh();
}
