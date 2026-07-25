using UnityEngine;

// Attach to a door GameObject with a Collider, pivoted at the hinge edge (not
// the center). Press E to swing it open/closed; a Switch can also call Toggle()
// directly. No animation clips — the swing is a plain procedural rotation.
[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openSpeed = 120f;

    public string InteractLabel => _isOpen ? "Close" : "Open";

    private bool        _isOpen;
    private float       _currentAngle;
    private Quaternion  _closedRotation;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
    }

    private void Update()
    {
        float targetAngle = _isOpen ? _openAngle : 0f;
        if (Mathf.Approximately(_currentAngle, targetAngle)) return;

        _currentAngle = Mathf.MoveTowards(_currentAngle, targetAngle, _openSpeed * Time.deltaTime);
        transform.localRotation = _closedRotation * Quaternion.Euler(0f, _currentAngle, 0f);
    }

    public void Interact(GameObject player) => Toggle();

    public void Toggle() => _isOpen = !_isOpen;
}
