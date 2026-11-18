using System;
using UnityEngine;

namespace CGD.Editor
{
    // One colour of the placeholder kit and what it stands for, so the same thing is the same
    // colour in every prop (fuel red, coolant blue, machinery yellow).
    [Serializable]
    public class PlaceholderSwatch
    {
        public string Name = "Swatch";
        public Material Material;
        [Tooltip("What this colour marks, shown when picking it")]
        public string Use;
    }
}
