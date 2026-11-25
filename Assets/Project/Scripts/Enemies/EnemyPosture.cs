using System.Collections.Generic;
using UnityEngine;

namespace CGD.Enemies
{
    // Whether an enemy stands or crouches, and its body shown that way: the parts of it that carry
    // a mesh or a collider (body, head, visor…) squashed down toward its feet, so its hitboxes
    // duck behind low cover with it and shots over the cover miss. Its eyes stay where they are:
    // crouched at cover it peeks over the top, as anyone ducking between shots does.
    //
    // Changes over a quarter of a second rather than at once.
    internal sealed class EnemyPosture
    {
        // A crouched enemy's height against its standing one: a 1.5 m figure ducks to about 0.9 m,
        // under a counter, a crate or a low wall (as the room planner reads cover, SightBlocking).
        public const float CrouchedShare = 0.6f;
        private const float ChangePerSecond = 4f;

        private readonly List<(Transform part, Vector3 position, Vector3 scale)> _parts = new();
        private float _share = 1f;

        public EnemyPosture(Transform root)
        {
            foreach (Transform part in root)
                if (part.TryGetComponent(out Renderer _) || part.TryGetComponent(out Collider _))
                    _parts.Add((part, part.localPosition, part.localScale));
        }

        public bool IsCrouching => _share < 1f;
        // How tall it stands now against its full height (1 standing, CrouchedShare crouched).
        public float HeightShare => _share;

        public void Tick(bool crouch, float deltaTime)
        {
            float target = crouch ? CrouchedShare : 1f;
            if (Mathf.Approximately(_share, target)) return;

            _share = Mathf.MoveTowards(_share, target, ChangePerSecond * deltaTime);
            Apply();
        }

        // Straight up, at once (a new life, a reset).
        public void Stand()
        {
            if (!IsCrouching) return;
            _share = 1f;
            Apply();
        }

        private void Apply()
        {
            foreach ((Transform part, Vector3 position, Vector3 scale) in _parts)
            {
                if (part == null) continue;
                part.localPosition = new Vector3(position.x, position.y * _share, position.z);
                part.localScale    = new Vector3(scale.x, scale.y * _share, scale.z);
            }
        }
    }
}
