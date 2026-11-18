using UnityEngine;

namespace CGD.Enemies
{
    // How a faction carries itself, apart from how a single enemy type is built (EnemyData
    // › Doctrine): what shakes its nerve and what it does when it breaks, how it raises the
    // alarm, how it idles, how it winds down after a fight, what it gives away before one and
    // how it uses the ground it has learned and finds its own way round. Shared by every enemy of
    // the faction, whichever behavior tree it runs. Empty = a plain enemy.
    [CreateAssetMenu(fileName = "NewFactionDoctrine", menuName = "CGD/Enemies/Faction Doctrine")]
    public class FactionDoctrine : ScriptableObject
    {
        private static FactionDoctrine _neutral;

        [SerializeField] private MoraleDoctrine     _morale     = new();
        [SerializeField] private EscalationDoctrine _escalation = new();
        [SerializeField] private IdleDoctrine       _idle       = new();
        [SerializeField] private EncounterDoctrine  _encounter  = new();
        [SerializeField] private OverseerDoctrine   _overseer   = new();
        [SerializeField] private TemperamentDoctrine _temperament = new();
        [SerializeField] private ForesightDoctrine  _foresight  = new();
        [SerializeField] private ReflexDoctrine     _reflexes    = new();
        [SerializeField] private AftermathDoctrine  _aftermath  = new();
        [SerializeField] private TellDoctrine       _tells      = new();
        [SerializeField] private GroundDoctrine     _ground     = new();
        [SerializeField] private TerritoryDoctrine  _territory  = new();
        [SerializeField] private PatrolDoctrine     _patrol     = new();

        public MoraleDoctrine     Morale     => _morale;
        public EscalationDoctrine Escalation => _escalation;
        public IdleDoctrine       Idle       => _idle;
        public EncounterDoctrine  Encounter  => _encounter;
        public OverseerDoctrine   Overseer   => _overseer;
        public TemperamentDoctrine Temperament => _temperament;
        public ForesightDoctrine  Foresight  => _foresight;
        public ReflexDoctrine     Reflexes   => _reflexes;
        public AftermathDoctrine  Aftermath  => _aftermath;
        public TellDoctrine       Tells      => _tells;
        public GroundDoctrine     Ground     => _ground;
        public TerritoryDoctrine  Territory  => _territory;
        public PatrolDoctrine     Patrol     => _patrol;

        // What an enemy without a doctrine follows: every default, no differences.
        public static FactionDoctrine Neutral =>
            _neutral != null ? _neutral : _neutral = CreateInstance<FactionDoctrine>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _neutral = null;
    }
}
