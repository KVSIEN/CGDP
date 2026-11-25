using UnityEngine;

namespace CGD.Algorithms
{
    // A damped spring: a value that chases a target the way a weight on a spring would, with
    // speed and follow-through instead of snapping or sliding at a constant rate.
    //
    // How it works: every update, the spring pulls the value toward the target with a force that
    // grows with the distance (stiffness, set here by `frequency`), and a damper pushes against
    // the value's speed (`dampingRatio`). The force changes the velocity, the velocity changes the
    // value ("semi-implicit Euler", in small fixed steps so it stays stable when frames are slow).
    //   dampingRatio 1    critically damped: as fast as possible with no overshoot;
    //   dampingRatio < 1  overshoots and wobbles before settling (0.3 is bouncy, 0.6 lively);
    //   dampingRatio > 1  sluggish, creeps in;
    //   frequency         how quick, in wobbles per second (2–5 feels responsive).
    //
    // Why it's useful: motion that feels physical and reacts naturally when the target changes
    // mid-way, which a fixed Lerp or tween can't. Weapon sway and recoil recovery, camera follow,
    // UI that pops and settles, a health bar's trailing chunk, a creature's head tracking the
    // player, squash and stretch. Use one per axis, or a SpringDamper for each of x, y and z.
    public sealed class SpringDamper
    {
        // Updates longer than this are split into several steps of at most this long.
        private const float MaxStep = 1f / 120f;

        public SpringDamper(float frequency, float dampingRatio, float startValue = 0f)
        {
            Frequency    = frequency;
            DampingRatio = dampingRatio;
            Value        = startValue;
        }

        public float Frequency    { get; set; }
        public float DampingRatio { get; set; }
        public float Value        { get; set; }
        public float Velocity     { get; set; }

        // Moves the value `deltaTime` seconds toward `target`, and returns it.
        public float Update(float target, float deltaTime)
        {
            float angularFrequency = 2f * Mathf.PI * Frequency;
            while (deltaTime > 0f)
            {
                float step = Mathf.Min(deltaTime, MaxStep);
                float springForce = angularFrequency * angularFrequency * (target - Value);
                float damperForce = -2f * DampingRatio * angularFrequency * Velocity;
                Velocity += (springForce + damperForce) * step;
                Value    += Velocity * step;
                deltaTime -= step;
            }
            return Value;
        }

        // Jumps straight to a value, at rest.
        public void Reset(float value)
        {
            Value    = value;
            Velocity = 0f;
        }
    }
}
