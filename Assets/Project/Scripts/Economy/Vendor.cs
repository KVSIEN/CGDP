using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Economy
{
    // Someone who trades: owns a Shop stocked from a ShopCatalog. It has no interaction of
    // its own. Something else opens it: a dialogue choice (OpenShopDialogueAction), a
    // Switch or EventInteractable calling OpenFor, a script. The shop panel lives on the
    // HUD and listens for Opened, so vendors can be prefabs.
    public class Vendor : MonoBehaviour, ISeedable
    {
        [SerializeField] private string _displayName = "Vendor";
        [SerializeField] private ShopCatalog _catalog;
        [Tooltip("Same seed → same shelf every play. Empty = random, or the level's seed when a generated level places it")]
        [SerializeField] private string _seed;
        [Tooltip("Seconds between restocks. 0 = stock once")]
        [SerializeField, Min(0f)] private float _restockInterval;

        private Seed  _baseSeed;
        private int   _restocks;
        private float _restockTimer;

        // (vendor, the customer)
        public static event Action<Vendor, ShopCustomer> Opened;

        public string DisplayName => _displayName;
        public Shop   Shop        { get; private set; }

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"{name}: Vendor has no ShopCatalog.", this);
                enabled = false;
                return;
            }

            _baseSeed = string.IsNullOrWhiteSpace(_seed) ? Seed.Random() : Seed.Parse(_seed);
            Shop = new Shop(_catalog, _catalog.Prices != null ? null : ScriptableObject.CreateInstance<PriceTable>());
            Restock();
        }

        private void Update()
        {
            if (_restockInterval <= 0f) return;

            _restockTimer += Time.deltaTime;
            if (_restockTimer < _restockInterval) return;

            _restockTimer = 0f;
            Restock();
        }

        public void Restock() => Shop?.Restock(_baseSeed.Derive(_restocks++));

        // From whatever placed the vendor; a seed typed on the vendor itself wins.
        public void ApplySeed(Seed seed)
        {
            if (!string.IsNullOrWhiteSpace(_seed)) return;

            _baseSeed     = seed.Derive("stock");
            _restocks     = 0;
            _restockTimer = 0f;
            Restock();
        }

        // False when the actor can't trade (no inventory) or the vendor isn't set up.
        public bool OpenFor(GameObject actor)
        {
            if (Shop == null || !ShopCustomer.TryFrom(actor, out ShopCustomer customer)) return false;

            Opened?.Invoke(this, customer);
            return true;
        }
    }
}
