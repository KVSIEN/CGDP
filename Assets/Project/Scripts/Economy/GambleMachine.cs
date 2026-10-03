using UnityEngine;
using CGD.Combat;
using CGD.Feedback;
using CGD.Interaction;
using CGD.Loot;
using CGD.Player;

namespace CGD.Economy
{
    // A slot machine in a Gamble room: pay credits (or blood) for a pull at its loot. A
    // pull busts, wins a normal drop, or hits the jackpot (a drop rolled with extra luck).
    // Each pull costs more than the last, and the machine runs dry after a few.
    [RequireComponent(typeof(Collider))]
    public class GambleMachine : MonoBehaviour, IInteractable
    {
        [SerializeField] private GamblePayment _payment = GamblePayment.Credits;
        [SerializeField] private CurrencyDefinition _currency;
        [Tooltip("Price of the first pull, in credits")]
        [SerializeField, Min(1)] private int _basePrice = 30;
        [Tooltip("Health taken by the first pull, as a share of the player's maximum")]
        [SerializeField, Range(0.01f, 0.9f)] private float _healthCost = 0.15f;
        [SerializeField, Min(1)] private int _pulls = 3;
        [SerializeField] private GambleOdds _odds = new();
        [SerializeField, Min(0f)] private float _jackpotLuck = 3f;
        [SerializeField] private LootDropper _dropper;
        [SerializeField, Min(0f)] private float _holdDuration = 0.6f;

        private int _pulled;

        public float HoldDuration => _holdDuration;

        public bool CanInteract(GameObject interactor) => _pulled < _pulls && _dropper != null;

        public string GetInteractLabel(GameObject interactor) => _payment == GamblePayment.Credits
            ? $"Gamble  ({(_currency != null ? _currency.Format(CreditPrice) : CreditPrice.ToString())})"
            : $"Gamble  ({HealthPrice(interactor)} health)";

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (!TryPay(interactor))
            {
                FeedbackBus.Notify(_payment == GamblePayment.Credits ? "Not enough credits" : "Too weak to pay in blood", NotificationStyle.Warning);
                return;
            }

            _pulled++;
            switch (_odds.Resolve(Random.value))
            {
                case GambleResult.Bust:
                    FeedbackBus.Notify("Bust", NotificationStyle.Warning);
                    break;
                case GambleResult.Win:
                    _dropper.Drop();
                    FeedbackBus.Notify("Winner", NotificationStyle.Reward);
                    break;
                case GambleResult.Jackpot:
                    _dropper.AddLuck(_jackpotLuck);
                    _dropper.Drop();
                    _dropper.AddLuck(-_jackpotLuck);
                    FeedbackBus.Notify("JACKPOT!", NotificationStyle.Reward);
                    break;
            }
            if (_pulled >= _pulls) FeedbackBus.Notify("The machine runs dry");
        }

        private int CreditPrice => _odds.PriceFor(_basePrice, _pulled);

        private int HealthPrice(GameObject interactor) =>
            interactor != null && interactor.TryGetComponent(out HealthManager health)
                ? _odds.PriceFor(Mathf.CeilToInt(health.MaxHealth * _healthCost), _pulled)
                : 0;

        private bool TryPay(GameObject interactor)
        {
            if (_payment == GamblePayment.Credits)
                return _currency != null && interactor.TryGetComponent(out PlayerInventory inventory)
                    && Wallet.TryPay(inventory.Inventory, _currency, CreditPrice);

            // Blood money never kills: the pull is refused if it would.
            if (!interactor.TryGetComponent(out HealthManager health)) return false;
            int price = HealthPrice(interactor);
            if (health.Health <= price) return false;
            health.TakeDamage(new DamageInfo(price, armorPenetration: 1f));
            return true;
        }
    }
}
