namespace CGD.Combat
{
    public interface IActionEvent
    {
        int StartFrame { get; }
        // -1 = single-frame (instant). OnEnter + OnExit on the same frame, no OnTick.
        int EndFrame { get; }

        void OnEnter(ActionContext ctx);
        void OnTick(ActionContext ctx);
        void OnExit(ActionContext ctx);
    }
}
