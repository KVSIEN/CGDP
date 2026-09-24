using System;
using System.Collections.Generic;

namespace CGD.Stats
{
    // Every modifier on one stat, with the combined result cached so applying it is
    // cheap enough to do every frame. The formula lives in StatModifierOp.
    public class ModifierStack
    {
        private readonly List<Modifier> _modifiers = new();

        private float _additive;
        private float _multiplicative;
        private float _compound = 1f;
        private bool  _hasOverride;
        private float _override;

        public int Count => _modifiers.Count;

        public IReadOnlyList<Modifier> Modifiers => _modifiers;

        public event Action Changed;

        public void Add(Modifier modifier)
        {
            _modifiers.Add(modifier);
            Recalculate();
        }

        public int RemoveFrom(object source)
        {
            int removed = _modifiers.RemoveAll(m => Equals(m.Source, source));
            if (removed > 0) Recalculate();
            return removed;
        }

        public void Clear()
        {
            if (_modifiers.Count == 0) return;

            _modifiers.Clear();
            Recalculate();
        }

        public float Apply(float baseValue) =>
            _hasOverride ? _override : (baseValue + _additive) * (1f + _multiplicative) * _compound;

        private void Recalculate()
        {
            _additive       = 0f;
            _multiplicative = 0f;
            _compound       = 1f;
            _hasOverride    = false;

            foreach (Modifier modifier in _modifiers)
            {
                switch (modifier.Op)
                {
                    case StatModifierOp.Additive:       _additive       += modifier.Value; break;
                    case StatModifierOp.Multiplicative: _multiplicative += modifier.Value; break;
                    case StatModifierOp.Compound:       _compound       *= 1f + modifier.Value; break;
                    case StatModifierOp.Override:
                        _hasOverride = true;
                        _override    = modifier.Value;
                        break;
                }
            }

            Changed?.Invoke();
        }
    }
}
