using System.Collections;
using UnityEngine;

namespace CGD.Combat
{
    // Brings a character back where it stands some time after it dies, at full health. For
    // things that must keep being shot at, such as the firing range's target dummies. While
    // dead its body (every child object) is hidden, so shots pass through the empty spot.
    [RequireComponent(typeof(HealthManager))]
    public class RespawnOnDeath : MonoBehaviour
    {
        [Tooltip("Seconds between dying and standing there again")]
        [SerializeField, Min(0f)] private float _delay = 4f;

        private HealthManager _health;

        private void Awake()
        {
            _health = GetComponent<HealthManager>();
            _health.OnDeath += HandleDeath;
        }

        private void OnDestroy() => _health.OnDeath -= HandleDeath;

        private void HandleDeath() => StartCoroutine(RespawnAfterDelay());

        private IEnumerator RespawnAfterDelay()
        {
            SetBodyActive(false);
            yield return new WaitForSeconds(_delay);
            SetBodyActive(true);
            _health.Revive();
        }

        private void SetBodyActive(bool active)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(active);
        }
    }
}
