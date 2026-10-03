namespace CGD.Expedition
{
    // How a run ended and what it cost or paid. Counts are item units: a stack of 30
    // rounds counts 30, each gear piece counts 1.
    public readonly struct RunReport
    {
        public RunReport(RunOutcome outcome, int carried, int brought)
        {
            Outcome = outcome;
            Carried = carried;
            Brought = brought;
        }

        public RunOutcome Outcome { get; }
        // Everything the player had when the run ended: stored on extraction, lost on death.
        public int Carried { get; }
        // What came from the ship's storage at the start of the run.
        public int Brought { get; }

        public bool Extracted => Outcome == RunOutcome.Extracted;
    }
}
