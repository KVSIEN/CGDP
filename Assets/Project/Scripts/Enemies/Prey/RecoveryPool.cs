using UnityEngine;

namespace CGD.Enemies
{
    // Health owed to a creature that it wins back gradually: damage taken from a predator is
    // banked as a share, then paid out at a steady rate. Plain C# so the rule is testable.
    internal sealed class RecoveryPool
    {
        private readonly float _share;
        private readonly float _ratePerSecond;
        private readonly float _cap;

        public RecoveryPool(float share, float ratePerSecond, float cap)
        {
            _share         = Mathf.Max(0f, share);
            _ratePerSecond = Mathf.Max(0f, ratePerSecond);
            _cap           = Mathf.Max(0f, cap);
        }

        public float Owed { get; private set; }

        public void Bank(float damage) => Owed = Mathf.Min(_cap, Owed + Mathf.Max(0f, damage) * _share);

        // What to heal this frame.
        public float Pay(float deltaTime)
        {
            float paid = Mathf.Min(Owed, _ratePerSecond * deltaTime);
            Owed -= paid;
            return paid;
        }

        public void Clear() => Owed = 0f;
    }
}
