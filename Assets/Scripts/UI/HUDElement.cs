using UnityEngine;

public abstract class HUDElement : MonoBehaviour
{
    public bool IsVisible { get; protected set; } = true;

    public virtual void Show()
    {
        IsVisible = true;
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        IsVisible = false;
        gameObject.SetActive(false);
    }

    public virtual void Toggle()
    {
        if (IsVisible) Hide(); else Show();
    }

    public abstract void Refresh();
}
