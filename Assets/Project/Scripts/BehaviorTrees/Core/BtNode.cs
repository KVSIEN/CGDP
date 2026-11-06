using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A node of a behavior tree. The tree asset holds each node once as authored data;
    // every agent runs its own copy (see BehaviorTree), so a node may keep per-run state
    // in fields marked [NonSerialized].
    //
    // Lifecycle: OnInitialize once per copy, then for each run OnEnter → OnTick (every
    // tick while Running) → OnExit, which also runs when a parent aborts the node.
    // Reference-type state must be created in OnInitialize, not in a field initializer:
    // copies are shallow, so an initializer's object would be shared by every agent.
    [Serializable]
    public abstract class BtNode
    {
        [SerializeField, HideInInspector] private string  _guid = NewGuid();
        [SerializeField, HideInInspector] private Vector2 _position;
        [Tooltip("Designer notes, shown on the node in the editor")]
        [SerializeField, TextArea(1, 3)]  private string  _comment;

        [NonSerialized] private bool _started;

        public string Guid    => _guid;
        public string Comment => _comment;

        // Editor layout only.
        internal Vector2 Position { get => _position; set => _position = value; }

        public bool     IsRunning  => _started;
        // Debugging: the last status and the tick it was reported on (see BtContext.TickCount).
        public BtStatus LastStatus { get; private set; }
        public int      LastTick   { get; private set; } = -1;

        // A short line for the editor describing this node's settings ("Wait 1–2 s").
        public virtual string Summary => null;

        public BtStatus Tick(BtContext context)
        {
            if (!_started)
            {
                _started = true;
                OnEnter(context);
            }

            BtStatus status = OnTick(context);
            LastStatus = status;
            LastTick   = context.TickCount;

            if (status != BtStatus.Running)
            {
                _started = false;
                OnExit(context);
            }
            return status;
        }

        // Stops a running node (and its running children) without letting it finish.
        public void Abort(BtContext context)
        {
            if (!_started) return;

            OnAbort(context);
            _started   = false;
            LastStatus = BtStatus.Failure;
            OnExit(context);
        }

        protected virtual void OnInitialize(BtContext context) { }
        protected virtual void OnEnter(BtContext context) { }
        protected abstract BtStatus OnTick(BtContext context);
        protected virtual void OnExit(BtContext context) { }
        // Runs before OnExit when the node is aborted; parents abort their children here.
        protected virtual void OnAbort(BtContext context) { }

        internal void Initialize(BtContext context) => OnInitialize(context);

        // A per-agent copy with the authored settings and fresh runtime state.
        internal BtNode CreateRuntimeCopy()
        {
            var copy = (BtNode)MemberwiseClone();
            copy._started  = false;
            copy.LastTick  = -1;
            return copy;
        }

        // An authoring duplicate (copy/paste): same settings, a new identity, no children.
        internal virtual BtNode Duplicate()
        {
            var copy = (BtNode)MemberwiseClone();
            copy._guid = NewGuid();
            return copy;
        }

        private static string NewGuid() => System.Guid.NewGuid().ToString("N");
    }
}
