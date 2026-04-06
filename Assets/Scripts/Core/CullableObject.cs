using UnityEngine;

/// <summary>
/// Add to any GameObject that should be excluded from rendering when out of camera view.
/// Registers itself with VisibilityCullingManager automatically.
/// </summary>
public class CullableObject : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    [SerializeField, Min(1)] private int _showStableChecks = 1;
    [SerializeField, Min(1)] private int _hideStableChecks = 2;

    public bool IsVisible { get; private set; } = true;
    public Bounds WorldBounds { get; private set; }

    private bool _hasPendingVisibility;
    private bool _pendingVisibility;
    private int _pendingVisibilityChecks;

    private void Awake()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();
        RefreshBounds();
    }

    private void OnEnable()  => VisibilityCullingManager.Register(this);
    private void OnDisable() => VisibilityCullingManager.Unregister(this);

    public void RefreshBounds()
    {
        if (_renderers.Length == 0) return;
        Bounds b = _renderers[0].bounds;
        for (int i = 1; i < _renderers.Length; i++)
            if (_renderers[i] != null) b.Encapsulate(_renderers[i].bounds);
        WorldBounds = b;
    }

    public void ApplyVisibilitySample(bool visible)
    {
        if (visible == IsVisible)
        {
            _hasPendingVisibility = false;
            _pendingVisibilityChecks = 0;
            return;
        }

        if (!_hasPendingVisibility || _pendingVisibility != visible)
        {
            _hasPendingVisibility = true;
            _pendingVisibility = visible;
            _pendingVisibilityChecks = 1;
        }
        else
        {
            _pendingVisibilityChecks++;
        }

        int requiredChecks = visible ? _showStableChecks : _hideStableChecks;
        if (_pendingVisibilityChecks < requiredChecks) return;

        SetVisible(visible);
        _hasPendingVisibility = false;
        _pendingVisibilityChecks = 0;
    }

    private void SetVisible(bool visible)
    {
        if (visible == IsVisible) return;
        IsVisible = visible;
        foreach (var r in _renderers)
            if (r != null) r.enabled = visible;
    }
}
