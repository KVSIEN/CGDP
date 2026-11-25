namespace CGD.Algorithms
{
    // Domain warping: sample one noise at a point that a second noise has pushed about.
    //
    // How it works: before asking the source noise about (x, y), ask the warp noise twice (at two
    // unrelated spots, one for each axis) how far to push the point, and sample the source there
    // instead. Where the warp is gentle the source barely changes; where it is strong, the
    // source's shapes are stretched, twisted and swirled.
    //
    // Why it's useful: noise on its own looks like noise. Warped, it looks grown or flowed:
    // creeping overgrowth, marble, flowing lava, smoke, wood grain, rivers that meander,
    // coastlines with bays and inlets. It is cheap (two extra samples) and works with any noise;
    // warping fractal noise with fractal noise gives the richest look (Inigo Quilez, "Domain
    // warping", 2002).
    public sealed class DomainWarpNoise : INoise2D
    {
        // Where the second warp sample is taken: far enough from the first that the two don't match.
        private const float SecondAxisX = 41.7f;
        private const float SecondAxisY = 23.3f;

        private readonly INoise2D _source;
        private readonly INoise2D _warp;
        private readonly float _strength;

        // `strength`: how far, in the source's units, the point may be pushed. About 0.5 to 2 times
        // the feature size is a good start.
        public DomainWarpNoise(INoise2D source, INoise2D warp, float strength)
        {
            _source   = source;
            _warp     = warp;
            _strength = strength;
        }

        public float Sample(float x, float y)
        {
            float pushX = _warp.Sample(x, y) * _strength;
            float pushY = _warp.Sample(x + SecondAxisX, y + SecondAxisY) * _strength;
            return _source.Sample(x + pushX, y + pushY);
        }
    }
}
