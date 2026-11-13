using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using CGD.Abilities;
using CGD.BehaviorTrees;
using CGD.Combat;
using CGD.Core;
using CGD.Impacts;
using CGD.Stealth;
using CGD.Stats;

namespace CGD.Enemies
{
    // An enemy's body and senses — movement, perception, melee and ranged attacks — plus
    // the behavior tree (EnemyData.BehaviorTree) that decides what to do with them. Stuns
    // and death are handled here, outside the tree.
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(Stunnable))]
    [DisallowMultipleComponent]
    public class EnemyAI : MonoBehaviour, IPoolable, IBehaviorTreeOwner, IBtAgentStatus
    {
        // Investigate: curious, checking something out. Hunt: knows a hostile is about and is
        // searching for it. Return: heading back to its post after giving up (or a kill).
        // Tree nodes report which of these they are doing, for visuals, animation and audio.
        public enum AiState { Patrol, Investigate, Hunt, Return, Chase, Retreat, RaiseAlarm, Stunned, Dead }

        private const float CorneredDistance = 3f;

        [SerializeField] private EnemyData   _data;
        [SerializeField] private Transform[] _waypoints;
        [Tooltip("Layers hostile characters can be found on")]
        [SerializeField] private LayerMask   _targetMask = ~0;
        [Tooltip("Geometry that blocks line of sight")]
        [SerializeField] private LayerMask   _obstacleMask;

        private static readonly ProfilerMarker SensesMarker = new("EnemyAI.Senses");

        private EnemyHealth  _health;
        private Stunnable    _stunnable;
        private DamageSource _damageSource;
        private CharacterStats _stats;
        private Stealthable  _stealth;

        // The brain: the tree, the state its nodes last reported (for visuals), and time
        // gathered since its last tick (calm enemies think less often, see TreeTickRate).
        private BehaviorTree _tree;
        private AiState      _state = AiState.Patrol;
        private AiState      _stateBeforeStun;
        private float        _sinceTreeTick;
        private bool         _started;

        private float _nextStaggerAt;
        private EngageStep    _step;
        private HealthManager _engagedTarget;

        public event Action<AiState> StateChanged;
        // Melee wind-up begins (the hit lands AttackWindup seconds later).
        public event Action AttackStarted;
        // A ranged shot leaves the enemy.
        public event Action Fired;
        // What it is doing within a fight changed (for animation and audio).
        public event Action<EngageStep> StepChanged;
        // A heavy hit staggered it.
        public event Action Staggered;
        // A grenade left its hand.
        public event Action Threw;
        // Vanished from this position and reappeared where it now stands (Phase Shift).
        public event Action<Vector3> Relocated;
        // An ability it cast went off (after its wind-up).
        public event Action<Ability> AbilityUsed;
        // It gave a sign of what it just noticed or decided (see TellDoctrine), for visuals and audio.
        public event Action<TellKind> Told;

        public AiState State => _state;

        // The running tree (null when its EnemyData has none), for the tree editor's live view.
        public BehaviorTree Tree => _tree;

        // What it believes is going on (for the awareness indicator and stealth feedback).
        public AwarenessLevel Awareness      => Perception.Level;
        public float          AwarenessValue => Perception.Awareness;
        public bool           IsCautious     => Perception.IsCautious;
        public EngageStep     Step           => _step;
        public float          MoraleValue    => Morale.Value;

        internal NavMeshAgent      Agent        { get; private set; }
        // Destinations go through this, not Agent.SetDestination, so they route round hazards.
        internal EnemyRouting      Routing      { get; private set; }
        // Speed, facing and knockback.
        internal EnemyMotion       Motion       { get; private set; }
        internal EnemyPerception   Perception   { get; private set; }
        internal EnemyRangedCombat RangedCombat { get; private set; }
        internal EnemyMelee        Melee        { get; private set; }
        internal EnemySearch       Search       { get; private set; }
        internal EnemyTactics      Tactics      { get; private set; }
        internal EnemyPost         Post         { get; private set; }
        internal AlarmKnowledge    Alarms       { get; private set; }
        internal EnemyAbilities    Abilities    { get; private set; }
        internal EnemyAmbush       Ambush       { get; private set; }
        internal EnemyMinions      Minions      { get; private set; }
        internal EnemyBelief       Belief       { get; private set; }
        internal GrenadeThrower    Grenades     { get; private set; }
        internal EnemySquad        Squad        { get; private set; }
        // The fight it is part of with its kin (Encounter), and its job in it.
        internal Encounter         Encounter    { get; private set; }
        internal SquadRole         SquadRole    => Encounter != null ? Encounter.RoleOf(this) : SquadRole.None;
        internal EnemyMorale       Morale       { get; private set; }
        internal EnemyIdleLife     Idle         { get; private set; }
        internal EnemyTells        Tells        { get; private set; }
        internal EnemyForesight    Foresight    { get; private set; }
        internal EnemyReflexes     Reflexes     { get; private set; }
        internal EnemyVoice        Voice        { get; private set; }
        internal TargetReadings    Readings     { get; private set; }

        // How its faction carries itself; a plain doctrine without one.
        internal FactionDoctrine Doctrine => _data.Doctrine != null ? _data.Doctrine : FactionDoctrine.Neutral;

        // Shaken and not cornered: time to fall back. Cornered, it fights instead.
        internal bool ShouldRetreat => Morale.IsShaken && !IsCornered;
        internal bool IsCornered =>
            Perception.IsTargetDetected &&
            (Perception.TargetPosition - transform.position).sqrMagnitude < CorneredDistance * CorneredDistance;

        // Set when it gave up a search or won its fight: it should head back to its post.
        internal bool    ReturnPending   { get; private set; }
        internal bool    ReturnIsVictory { get; private set; }
        internal Vector3 ReturnLookAt    { get; private set; }

        internal EnemyHealth       Health       => _health;
        internal EnemyData         Data         => _data;
        internal Transform[]       Waypoints    => _waypoints ?? Array.Empty<Transform>();
        internal bool              IsAttacking  => Melee.IsAttacking;
        // Not while it is still taking in that it is in a fight (its faction's beat before the first blow).
        internal bool              CanAttack    => Melee.CanAttack && !Tells.IsCommitting;
        internal bool              IsStunned    => _stunnable.IsStunned;

        private void Awake()
        {
            Agent         = GetComponent<NavMeshAgent>();
            _health       = GetComponent<EnemyHealth>();
            _stunnable    = GetComponent<Stunnable>();
            _damageSource = DamageSource.Of(gameObject);
            TryGetComponent(out _stats);
            Routing       = new EnemyRouting(this);
            Motion        = new EnemyMotion(this, Agent, _stunnable, _stats);
            Perception    = new EnemyPerception(this, _data, _targetMask, _obstacleMask, _health.Team);
            Tells         = new EnemyTells(this);
            Idle          = new EnemyIdleLife(this);
            Foresight     = new EnemyForesight(this);
            Reflexes      = new EnemyReflexes(this);
            RangedCombat  = new EnemyRangedCombat(this, _damageSource, _obstacleMask);
            Melee         = new EnemyMelee(this, _damageSource, _targetMask, _stats);
            Search        = new EnemySearch(this);
            Tactics       = new EnemyTactics(this, _obstacleMask);
            Post          = new EnemyPost(this);
            Alarms        = new AlarmKnowledge(this);
            TryGetComponent(out _stealth);
            Ambush        = new EnemyAmbush(_stealth);
            Minions       = new EnemyMinions(this);
            Belief        = new EnemyBelief(this, _obstacleMask);
            Abilities     = new EnemyAbilities(this, _damageSource, _stealth, Minions);
            Grenades      = new GrenadeThrower(this, _damageSource, _obstacleMask);
            Morale        = new EnemyMorale(this);
            Voice         = new EnemyVoice(this);
            Readings      = new TargetReadings(this);
            _health.OnDeath += OnDeath;
        }

        // The tree is built here rather than in Awake: rolled affixes swap in their data
        // (UseData) after Awake. Killed before its first frame, the enemy stays dead. Without
        // a tree it stands where it is (only its senses run).
        private void Start()
        {
            _started = true;
            Post.Remember();
            if (_data.BehaviorTree != null) _tree = _data.BehaviorTree.CreateInstance(gameObject, this);
            // Spread first ticks so a level's worth of calm enemies don't all think on one frame.
            _sinceTreeTick = UnityEngine.Random.value * TreeTickRate.Remote;
            if (_health.IsDead) ReportState(AiState.Dead);
        }

        private void OnEnable()
        {
            EnemyRoster.Add(this);
            Noise.Emitted         += Perception.OnNoise;
            EnemyCallouts.Raised  += Perception.OnCallout;
            FallenAllies.Fell     += Perception.OnAllyFell;
            ImpactEvents.Occurred += Perception.OnImpact;
            _health.OnHit         += Perception.OnHit;
            _health.OnDamaged     += OnDamaged;
        }

        private void OnDisable()
        {
            EnemyRoster.Remove(this);
            Noise.Emitted         -= Perception.OnNoise;
            EnemyCallouts.Raised  -= Perception.OnCallout;
            FallenAllies.Fell     -= Perception.OnAllyFell;
            ImpactEvents.Occurred -= Perception.OnImpact;
            _health.OnHit         -= Perception.OnHit;
            _health.OnDamaged     -= OnDamaged;

            // Switched off on death too (the body stays a body). A despawned or switched-off one
            // is gone: it lets go of its cover and anything it held in the fight.
            if (gameObject.activeInHierarchy) return;
            FallenAllies.Remove(this);
            Tactics.ReleaseCover();
            Tactics.EndMeleeTurn();
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
        }

        private void OnDestroy()
        {
            _health.OnDeath -= OnDeath;
            FallenAllies.Remove(this);
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            EnemyCoordination.Tick(Time.time);
            Minions.Tick(Time.time);

            // A stun freezes attacks and senses as well as movement.
            if (!_stunnable.IsStunned)
            {
                using (SensesMarker.Auto())
                {
                    Melee.Tick(dt);
                    RangedCombat.Tick(dt);
                    Perception.Tick(dt);
                    Belief.Tick(Time.time);
                    Tactics.Tick(Time.time);
                    Morale.Tick(dt);
                    TrackEngagement();
                }
            }

            if (_tree != null) TickTree(dt);
            Routing.Tick(Time.time);
            Motion.Tick(dt);
        }

        // A stun pauses the tree where it is, so the enemy resumes what it was doing.
        private void TickTree(float dt)
        {
            if (_stunnable.IsStunned)
            {
                if (_state != AiState.Stunned) _stateBeforeStun = _state;
                Agent.isStopped = true;
                ReportState(AiState.Stunned);
                return;
            }

            if (_state == AiState.Stunned)
            {
                Agent.isStopped = false;
                ReportState(_stateBeforeStun);
            }

            // Getting out of a grenade's way takes the tree's place until it is clear.
            if (Reflexes.Tick(dt, Time.time)) return;

            _sinceTreeTick += dt;
            if (_sinceTreeTick < TreeTickInterval()) return;

            EnemyBlackboard.Publish(this, _tree.Blackboard);
            _tree.Tick(_sinceTreeTick);
            _sinceTreeTick = 0f;
        }

        // Every frame once anything has its attention; the moment its senses pick something
        // up the interval drops to zero, so a calm enemy reacts no later than a busy one.
        internal float TreeTickInterval()
        {
            bool engaged = Perception.Level != AwarenessLevel.Unaware || Perception.HasLead ||
                           Perception.IsUnderFire || IsAttacking || RangedCombat.IsBursting;
            return TreeTickRate.IntervalFor(engaged, Perception.SensesHostiles);
        }

        void IBtAgentStatus.DescribeStatus(List<(string Label, string Value)> rows) => EnemyStatusReport.Describe(this, rows);

        // For enemies spawned at runtime (level builder, dev console) instead of placed with a route.
        public void SetWaypoints(Transform[] waypoints) => _waypoints = waypoints;

        // Says something from its type's bark set, if the level isn't too noisy (the director's
        // narration, scripted moments). False when nothing was said.
        public bool Bark(BarkKind kind) => Voice.Say(kind);

        // Sends a calm enemy to have a look round an area (the director's patrol probes). False if
        // it already has something on its mind.
        public bool CheckArea(Vector3 area) => Perception.Nudge(area);

        // Sends the enemy to investigate a position, e.g. a reinforcement sent toward the player.
        public void AlertTo(Vector3 position, DamageSource source) => Perception.Alert(position, source);

        // Rolled affixes run the enemy on a modified copy of its data (perception keeps the original).
        public void UseData(EnemyData data) => _data = data;

        // A search found nothing: stay on edge and head back to its post.
        internal void GiveUpSearch()
        {
            Perception.GiveUp();
            Overseers.ReportEscape(this, Time.time);
            BeginReturn(victory: false, transform.position + transform.forward);
        }

        internal void FinishReturn() => ReturnPending = false;

        internal void ReportStep(EngageStep step)
        {
            if (_step == step) return;
            _step = step;
            StepChanged?.Invoke(step);
        }

        // Behavior tree nodes say what the enemy is doing, so its visor, animation and
        // stealth feedback match (patrolling, investigating, fighting).
        internal void ReportState(AiState state)
        {
            if (state != AiState.Chase) ReportStep(EngageStep.None);
            if (_state == state) return;

            // Out of the fight: give the melee turn to an ally (a stun keeps it).
            if (_state == AiState.Chase && state != AiState.Stunned) Tactics.EndMeleeTurn();
            _state = state;
            StateChanged?.Invoke(state);
        }

        // What blocks sight and shots, for nodes that need to know whether a view is open.
        internal LayerMask ObstacleMask => _obstacleMask;

        internal void TryStartAttack()
        {
            if (Melee.TryStart()) AttackStarted?.Invoke();
        }

        internal void NotifyFired()
        {
            Reveal();
            Fired?.Invoke();
        }

        internal void NotifyThrew()
        {
            Reveal();
            Threw?.Invoke();
        }

        // Attacking and casting give away an enemy hiding in cover (or cloaked).
        internal void Reveal() => _stealth?.Reveal();

        internal void NotifyRelocated(Vector3 from) => Relocated?.Invoke(from);

        internal void NotifyAbilityUsed(Ability ability) => AbilityUsed?.Invoke(ability);

        internal void NotifyTold(TellKind kind)
        {
            if (kind != TellKind.None) Told?.Invoke(kind);
        }

        internal void JoinSquad(EnemySquad squad) => Squad = squad;

        internal void JoinEncounter(Encounter encounter) => Encounter = encounter;

        internal void LeaveEncounter(Encounter encounter)
        {
            if (Encounter == encounter) Encounter = null;
        }

        // Which layers hostile characters are found on (its overseer looks for them too).
        internal LayerMask TargetMask => _targetMask;

        // Remembers who it is fighting so it notices when that fight is won.
        private void TrackEngagement()
        {
            if (Perception.IsTargetDetected)
            {
                _engagedTarget = Perception.Target;
            }
            else if (_engagedTarget != null && _engagedTarget.IsDead)
            {
                BeginReturn(victory: true, _engagedTarget.transform.position);
                Perception.StayOnEdge(Perception.VictoryEdgeSeconds);
                _engagedTarget = null;
            }
            else if (Perception.Level == AwarenessLevel.Unaware)
            {
                _engagedTarget = null;
            }
        }

        private void BeginReturn(bool victory, Vector3 lookAt)
        {
            ReturnPending   = true;
            ReturnIsVictory = victory;
            ReturnLookAt    = lookAt;
            Alarms.Forget();
        }

        // A heavy hit knocks it off balance: a short stun that also spoils a wind-up.
        private void OnDamaged(float amount)
        {
            Morale.OnDamaged();
            if (_health.IsDead || Time.time < _nextStaggerAt) return;
            if (amount < _health.MaxHealth * _data.StaggerThreshold) return;

            _nextStaggerAt = Time.time + _data.StaggerCooldown;
            Melee.Interrupt();
            RangedCombat.CancelBurst();
            Abilities.Cancel();
            Grenades.Cancel();
            _stunnable.ApplyStun(_data.StaggerDuration);
            Staggered?.Invoke();
        }

        private void OnDeath()
        {
            Minions.DismissAll();
            Belief.Clear();
            Reflexes.Cancel();
            RangedCombat.CancelBurst();
            Search.Cancel();
            Tactics.ReleaseCover();
            Tactics.EndMeleeTurn();
            AttackTokens.ReleaseAll(this);
            BoldMovePermits.Release(this);
            FallenAllies.Add(new FallenAlly(this, transform.position, _health.Team));
            _tree?.Abort();
            ReportState(AiState.Dead);
            Motion.StopTurning();
            Routing.Clear();
            Agent.enabled = false;
            enabled = false;
        }

        // Reused from PrefabPool: wake the agent back up at the spawn position and start its
        // tree over. The first spawn is handled by Start.
        public void OnSpawned()
        {
            if (!_started) return;

            Agent.enabled = true;
            Agent.Warp(transform.position);
            Routing.Clear();
            enabled      = true;
            Melee.Reset();
            RangedCombat.Reset();
            Ambush.Disarm();
            Minions.DismissAll();
            Search.Cancel();
            Tactics.Reset();
            Perception.Reset();
            Readings.Forget();
            Belief.Clear();
            Morale.Reset();
            Tells.Reset();
            Idle.Cancel();
            Foresight.Reset();
            Reflexes.Cancel();
            Voice.Reset();
            Abilities.Reset();
            Post.Remember();
            FallenAllies.Remove(this);
            Motion.StopTurning();
            Alarms.Forget();
            ReturnPending  = false;
            _engagedTarget = null;
            _sinceTreeTick = 0f;

            _tree?.Reset();
            ReportState(AiState.Patrol);
        }
    }
}
