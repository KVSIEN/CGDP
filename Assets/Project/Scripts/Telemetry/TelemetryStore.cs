using CGD.Expedition;
using CGD.Saving;

namespace CGD.Telemetry
{
    // Reads and writes the lifetime totals. A run is added once, when it ends; nothing is
    // written mid-run, so a crash loses at most the run in progress.
    public class TelemetryStore
    {
        private readonly SaveFile _file;

        public LifetimeStats Lifetime { get; private set; } = new();

        public TelemetryStore(SaveFile file)
        {
            _file = file ?? throw new System.ArgumentNullException(nameof(file));
        }

        // A missing, unreadable or newer-than-known file starts from zero (the bad file is set aside, not overwritten).
        public void Load()
        {
            Lifetime = new LifetimeStats();
            if (!_file.Exists) return;

            if (_file.TryRead(out LifetimeStats saved) && saved.version >= 1 && saved.version <= LifetimeStats.CurrentVersion)
                Lifetime = saved;
            else
                _file.SetAsideUnreadable();
        }

        public bool Record(RunStats run, RunOutcome outcome)
        {
            Lifetime.Add(run, outcome);
            return _file.Write(Lifetime);
        }
    }
}
