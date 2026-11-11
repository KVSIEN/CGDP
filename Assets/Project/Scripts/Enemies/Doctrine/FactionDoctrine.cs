using UnityEngine;

namespace CGD.Enemies
{
    // How a faction carries itself, apart from how a single enemy type is built (EnemyData
    // › Doctrine): what shakes its nerve and what it does when it breaks, how it raises the
    // alarm, how it idles and what it gives away before it fights. Shared by every enemy of
    // the faction, whichever behavior tree it runs. Empty = a plain enemy.
    [CreateAssetMenu(fileName = "NewFactionDoctrine", menuName = "CGD/Enemies/Faction Doctrine")]
    public class FactionDoctrine : ScriptableObject
    {
        private static FactionDoctrine _neutral;

        [SerializeField] private MoraleDoctrine     _morale     = new();
        [SerializeField] private EscalationDoctrine _escalation = new();
        [SerializeField] private IdleDoctrine       _idle       = new();
        [SerializeField] private TellDoctrine       _tells      = new();

        public MoraleDoctrine     Morale     => _morale;
        public EscalationDoctrine Escalation => _escalation;
        public IdleDoctrine       Idle       => _idle;
        public TellDoctrine       Tells      => _tells;

        // What an enemy without a doctrine follows: every default, no differences.
        public static FactionDoctrine Neutral =>
            _neutral != null ? _neutral : _neutral = CreateInstance<FactionDoctrine>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _neutral = null;
    }
}
