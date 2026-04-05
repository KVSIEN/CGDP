using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float     _range        = 2.5f;
    [SerializeField] private LayerMask _interactMask = ~0;

    public bool   HasTarget   => _current != null;
    public string TargetLabel => _current?.InteractLabel ?? string.Empty;

    private PlayerInputHandler _input;
    private IInteractable      _current;

    // Pre-allocated to avoid per-frame heap allocations
    private readonly Collider[] _buffer = new Collider[16];

    private void Awake()
    {
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        FindClosest();

        if (_current != null && _input.WasPressed(GameAction.Interact))
            _current.Interact(gameObject);
    }

    private void FindClosest()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _range, _buffer, _interactMask, QueryTriggerInteraction.Collide);

        IInteractable best     = null;
        float         bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!_buffer[i].TryGetComponent<IInteractable>(out var candidate)) continue;

            float dist = (_buffer[i].transform.position - transform.position).sqrMagnitude;
            if (dist >= bestDist) continue;

            bestDist = dist;
            best     = candidate;
        }

        _current = best;
    }
}
