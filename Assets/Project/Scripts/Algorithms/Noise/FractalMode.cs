namespace CGD.Algorithms
{
    // How FractalNoise shapes each layer before adding it up.
    public enum FractalMode
    {
        // Plain layers: rolling hills with smaller bumps on them (fBm, "fractal Brownian motion").
        Standard,
        // Each layer folded at 0 so every valley becomes a soft crease: puffy clouds, billowing
        // smoke, lumpy rubble ("billow" or "turbulence").
        Billow,
        // Each layer folded and flipped so the folds become sharp crests: mountain ridges,
        // lightning, veins, cracks of lava, roots ("ridged multifractal", simplified).
        Ridged,
    }
}
