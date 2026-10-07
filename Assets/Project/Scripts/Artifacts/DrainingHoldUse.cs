using CGD.Meters;

namespace CGD.Artifacts
{
    // Base for hold uses that cost a resource every second they are up (a ward, a channel). It
    // starts only while the meter can pay, keeps draining while held, and stops itself when the
    // meter runs dry. Subclasses add what the hold actually does.
    public abstract class DrainingHoldUse : OffhandUse
    {
        private readonly MeterCost _drain;
        private float _ratio = 1f;
        private bool  _running;

        protected DrainingHoldUse(MeterCost drainPerSecond) => _drain = drainPerSecond;

        public override OffhandUseMode Mode => OffhandUseMode.Hold;
        public override float Ratio => _ratio;
        public override bool IsRunning => _running;

        public override bool Begin(OffhandContext ctx)
        {
            if (_running || !_drain.CanAfford(ctx.Meters) || !OnBegin(ctx)) return false;
            _running = true;
            return true;
        }

        public override bool Hold(OffhandContext ctx)
        {
            if (!_running) return false;
            if (!_drain.TryDrain(ctx.Meters, ctx.DeltaTime)) return false;

            OnHold(ctx);
            return true;
        }

        public override void End(OffhandContext ctx)
        {
            if (!_running) return;
            _running = false;
            OnEnd(ctx);
        }

        public override void Tick(OffhandContext ctx)
        {
            if (_drain.Meter != null && ctx.Meters != null && ctx.Meters.TryGet(_drain.Meter, out Meter meter))
                _ratio = meter.Ratio;
        }

        // Puts the effect up. Returns false if it can't be (e.g. nothing to reflect with).
        protected abstract bool OnBegin(OffhandContext ctx);
        protected virtual void OnHold(OffhandContext ctx) { }
        protected abstract void OnEnd(OffhandContext ctx);
    }
}
