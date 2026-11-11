using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Enemies
{
    // How a faction reacts at the two moments that matter: when it first notices something,
    // and when it commits to a fight. These are what make an enemy readable before it
    // attacks. The defaults change nothing.
    [Serializable]
    public sealed class TellDoctrine
    {
        [Header("Noticing something")]
        [SerializeField] private TellKind _noticedKind = TellKind.None;
        [Tooltip("Seconds it stops to take it in before going to look. Empty (0) = the profile's notice pause")]
        [SerializeField] private FloatRange _noticeHold = new(0f, 0f);
        [Tooltip("It turns to what it noticed with no in-between instead of swinging round")]
        [SerializeField] private bool _noticeSnapsToLead;

        [Header("Committing to a fight")]
        [SerializeField] private TellKind _committedKind = TellKind.None;
        [Tooltip("Seconds between making up its mind and its first attack. A second look at someone it was fighting moments ago is quicker")]
        [SerializeField] private FloatRange _hesitation = new(0f, 0f);

        [Header("Standing down")]
        [Tooltip("The sign it gives when it finally relaxes after a fight")]
        [SerializeField] private TellKind _standDownKind = TellKind.None;

        public TellKind StandDownKind     => _standDownKind;
        public TellKind NoticedKind       => _noticedKind;
        public FloatRange NoticeHold      => _noticeHold;
        public bool NoticeSnapsToLead     => _noticeSnapsToLead;
        public TellKind CommittedKind     => _committedKind;
        public FloatRange Hesitation      => _hesitation;
    }
}
