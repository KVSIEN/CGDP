using UnityEngine;

namespace CGD.Combat
{
    [CreateAssetMenu(fileName = "NewActionTimeline", menuName = "CGD/Combat/Action Timeline")]
    public class ActionTimeline : ScriptableObject
    {
        [Min(1)]
        public int TotalFrames = 10;

        [SerializeReference]
        public IActionEvent[] Events;

        public LayerMask HitMask = ~0;

        [Tooltip("Frame to emit AI perception noise (-1 = none)")]
        public int NoiseFrame = -1;

        [Min(0f)]
        public float NoiseRadius = 15f;
    }
}
