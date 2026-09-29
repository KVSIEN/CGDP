using UnityEngine;

namespace CGD.Animation
{
    // Parameter names every character Animator Controller can use. A controller only needs
    // the ones its clips care about — AnimatorBridge skips parameters a controller lacks.
    public static class AnimatorParams
    {
        // Floats
        public static readonly int Speed         = Animator.StringToHash("Speed");          // horizontal m/s
        public static readonly int ForwardSpeed  = Animator.StringToHash("ForwardSpeed");   // local, m/s
        public static readonly int StrafeSpeed   = Animator.StringToHash("StrafeSpeed");    // local, m/s
        public static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");  // m/s, up positive
        public static readonly int Aim           = Animator.StringToHash("Aim");            // 0 hip .. 1 aiming down sights

        // Bools
        public static readonly int Grounded  = Animator.StringToHash("Grounded");
        public static readonly int Crouching = Animator.StringToHash("Crouching");
        public static readonly int Sprinting = Animator.StringToHash("Sprinting");
        public static readonly int Sliding   = Animator.StringToHash("Sliding");
        public static readonly int Mantling  = Animator.StringToHash("Mantling");
        public static readonly int Rolling   = Animator.StringToHash("Rolling");
        public static readonly int Stunned   = Animator.StringToHash("Stunned");
        public static readonly int Reloading = Animator.StringToHash("Reloading");
        public static readonly int Alerted   = Animator.StringToHash("Alerted");
        public static readonly int Dead      = Animator.StringToHash("Dead");

        // Ints
        public static readonly int AttackIndex = Animator.StringToHash("AttackIndex");      // combo step, -1 = heavy

        // Triggers
        public static readonly int Attack = Animator.StringToHash("Attack");
        public static readonly int Fire   = Animator.StringToHash("Fire");
        public static readonly int Throw  = Animator.StringToHash("Throw");
        public static readonly int Hit    = Animator.StringToHash("Hit");
    }
}
