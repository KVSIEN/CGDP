namespace CGD.Algorithms
{
    // Anything that gives a smooth random value at any point of a 2D plane: the shape every noise
    // in this folder shares, so the tools that layer noise (FractalNoise, DomainWarpNoise) work on
    // any of them, and a system can be handed "some noise" without caring which.
    //
    // Sample returns a value from about -1 to 1 that changes smoothly: points close together get
    // values close together. The features (bumps, cells) are about 1 unit across; scale the input
    // to change that: Sample(x / 10, y / 10) gives features 10 units across. Convert to 0..1 with
    // NoiseMath.ToUnitRange.
    public interface INoise2D
    {
        float Sample(float x, float y);
    }
}
