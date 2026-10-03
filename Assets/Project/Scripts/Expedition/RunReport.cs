namespace CGD.Expedition
{
    // How a run ended and what it cost or paid. Counts are item units: a stack of 30
    // rounds counts 30, each gear piece counts 1.
    public readonly struct RunReport
    {
        public RunReport(RunOutcome outcome, int carried, int brought, int left = 0)
        {
            Outcome = outcome;
            Carried = carried;
            Brought = brought;
            Left    = left;
        }

        public RunOutcome Outcome { get; }
        // Everything the player had when the run ended: stored on extraction, lost on death.
        // After an emergency extraction: what made it into the hold.
        public int Carried { get; }
        // Emergency extraction only: what didn't fit in the escape pod.
        public int Left { get; }
        // What came from the ship's storage at the start of the run.
        public int Brought { get; }

        // Made it out, by the Exit or an escape pod.
        public bool Extracted => Outcome != RunOutcome.Died;
    }
}
