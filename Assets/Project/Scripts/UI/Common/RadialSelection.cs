using UnityEngine;

namespace CGD.UI
{
    // Picks a slice of a selection wheel from look input: mouse movement pushes a pointer
    // around the centre, a gamepad stick points directly. Slice 0 is at the top, the rest
    // follow clockwise. Inside the dead zone nothing is selected (release = cancel).
    public class RadialSelection
    {
        private const float MouseScale    = 0.01f;
        private const float DeadZone      = 0.35f;
        private const float StickDeadZone = 0.5f;

        private Vector2 _pointer;

        public int Count    { get; private set; }
        public int Selected { get; private set; } = -1;

        public void Reset(int count)
        {
            Count    = count;
            _pointer = Vector2.zero;
            Selected = -1;
        }

        public void Feed(Vector2 look, bool isStick)
        {
            if (Count <= 0) return;

            if (isStick)
            {
                // Letting go of the stick keeps the last choice.
                if (look.magnitude >= StickDeadZone) _pointer = look.normalized;
            }
            else
            {
                _pointer = Vector2.ClampMagnitude(_pointer + look * MouseScale, 1f);
            }

            Selected = _pointer.magnitude < DeadZone ? -1 : SliceAt(_pointer, Count);
        }

        public static int SliceAt(Vector2 direction, int count)
        {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;   // 0 = up, clockwise
            float slice = 360f / count;
            return Mathf.FloorToInt(Mathf.Repeat(angle + slice * 0.5f, 360f) / slice) % count;
        }
    }
}
