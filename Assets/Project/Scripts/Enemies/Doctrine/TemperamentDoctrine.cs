using System;
using UnityEngine;

namespace CGD.Enemies
{
    // A faction's temperament, for utility scoring: three axes every tree can weigh its
    // options by (a Score's Bias Key) — how eager it is to close and hit (Aggression), to keep
    // itself safe (Caution) and to work angles and tricks (Cunning). 1 = neutral. Its
    // situation moves them: regrouping or held back by its overseer it grows cautious, a
    // member who just stepped up grows bold (EnemyTemperament).
    [Serializable]
    public sealed class TemperamentDoctrine
    {
        [SerializeField, Range(0.2f, 2f)] private float _aggression = 1f;
        [SerializeField, Range(0.2f, 2f)] private float _caution    = 1f;
        [SerializeField, Range(0.2f, 2f)] private float _cunning    = 1f;

        public float Aggression => _aggression;
        public float Caution    => _caution;
        public float Cunning    => _cunning;
    }
}
