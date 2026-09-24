using System.Collections.Generic;

namespace CGD.Stats
{
    // Temporary modifiers — buffs, debuffs, a few seconds of overcharge — that remove
    // themselves from a ModifierSet when their time runs out. Tick it from game time
    // (CharacterStats uses GameClock) so durations pause and slow with the game.
    public class TimedModifiers<TKey>
    {
        private readonly ModifierSet<TKey>   _target;
        private readonly List<TimedModifier> _active = new();

        public TimedModifiers(ModifierSet<TKey> target) => _target = target;

        public int Count => _active.Count;

        // Every modifier in the preset lasts `seconds`, and the whole group ends together.
        public TimedModifier Add(IEnumerable<(TKey stat, StatModifierOp op, float value)> modifiers, float seconds)
        {
            var timed = new TimedModifier(Expire, seconds);
            foreach ((TKey stat, StatModifierOp op, float value) in modifiers)
                _target.Add(stat, new Modifier(op, value, timed));

            _active.Add(timed);
            return timed;
        }

        public TimedModifier Add(TKey stat, StatModifierOp op, float value, float seconds) =>
            Add(new[] { (stat, op, value) }, seconds);

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                TimedModifier timed = _active[i];
                timed.Remaining -= deltaTime;
                if (timed.Remaining <= 0f) Expire(timed);
            }
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                Expire(_active[i]);
        }

        private void Expire(TimedModifier timed)
        {
            if (!_active.Remove(timed)) return;

            timed.Remaining = 0f;
            _target.RemoveFrom(timed);
        }
    }
}
