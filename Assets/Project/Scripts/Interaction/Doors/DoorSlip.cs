using UnityEngine;

namespace CGD.Interaction
{
    // Whether someone is slipping past a sensor door rather than walking up to it: crouched,
    // and came into the sensed box from its side (along the wall) instead of from the front.
    // Standing up, or leaving the box, ends it; it can't start again until they've left.
    // Positions are in the sensor's space, the box centred on the door.
    public class DoorSlip
    {
        // How much further toward its side than its front a position must be to count as the side.
        private const float SideBias = 0.1f;

        private readonly Vector2 _half;
        private bool _inside;

        public DoorSlip(Vector3 boxSize) => _half = new Vector2(boxSize.x, boxSize.z) * 0.5f;

        public bool IsSlipping { get; private set; }

        public void Observe(Vector3 local, bool crouched)
        {
            if (!_inside)
            {
                _inside    = true;
                IsSlipping = crouched && Mathf.Abs(local.x) / _half.x > Mathf.Abs(local.z) / _half.y + SideBias;
                return;
            }
            if (!crouched) IsSlipping = false;
        }

        public void Left()
        {
            _inside    = false;
            IsSlipping = false;
        }
    }
}
