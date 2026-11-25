namespace CGD.Algorithms
{
    // Small helpers the noises share.
    public static class NoiseMath
    {
        // Eases t (0..1) so it starts and ends with zero slope: 6t⁵ - 15t⁴ + 10t³ (Ken Perlin's
        // "fade" curve). Blending grid corners with it hides the grid: without it the lines between
        // cells show up as creases.
        public static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        // A noise value (-1..1) moved to 0..1, for heights, chances and colours.
        public static float ToUnitRange(float value) => value * 0.5f + 0.5f;
    }
}
