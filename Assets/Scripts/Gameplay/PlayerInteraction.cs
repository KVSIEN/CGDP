using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float     _range           = 2.5f;
    [SerializeField] private LayerMask _interactMask    = ~0;
    [SerializeField] private Transform _forwardReference;   // assign camera transform

    public bool      HasTarget      => _current != null;
    public string    TargetLabel    => _current?.InteractLabel ?? string.Empty;
    public Vector3   TargetPosition => _currentTransform != null ? _currentTransform.position : Vector3.zero;

    private PlayerInputHandler _input;
    private IInteractable      _current;
    private Transform          _currentTransform;

    private readonly Collider[] _buffer = new Collider[16];

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        FindClosest();

        if (_current != null && _input.GetAction(GameAction.Interact))
            _current.Interact(gameObject);
    }

    private void FindClosest()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _range, _buffer, _interactMask, QueryTriggerInteraction.Collide);

        Vector3 forward    = _forwardReference != null ? _forwardReference.forward : transform.forward;
        Vector3 eyeOrigin  = _forwardReference != null ? _forwardReference.position : transform.position;

        IInteractable best          = null;
        float         bestScore    = float.MinValue;
        Transform     bestTransform = null;

        for (int i = 0; i < count; i++)
        {
            if (!_buffer[i].TryGetComponent<IInteractable>(out var candidate)) continue;

            Vector3 toTarget  = _buffer[i].transform.position - eyeOrigin;
            float   mag       = toTarget.magnitude;
            Vector3 dir       = mag > 0.05f ? toTarget / mag : forward;
            float   alignment = Vector3.Dot(forward, dir);
            if (alignment <= 0f) continue;

            // Score favours objects more aligned with look direction; distance is secondary.
            float score = alignment - mag / _range;
            if (score <= bestScore) continue;

            bestScore     = score;
            best          = candidate;
            bestTransform = _buffer[i].transform;
        }

        _current          = best;
        _currentTransform = bestTransform;
    }
}
