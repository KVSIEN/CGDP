using System;
using UnityEngine;

namespace CGD.Map
{
    // One room-to-be in the map graph. Holds only what the player experiences there;
    // links to other nodes live in MapGraph.Connections.
    [Serializable]
    public class MapNode
    {
        public const int NoFaction = -1;
        public const int NoSection = -1;

        [SerializeField] private int         _id;
        [SerializeField] private MapNodeType _type;
        [Tooltip("Editor/debug position only — not room geometry")]
        [SerializeField] private Vector2     _position;
        [SerializeField, Range(0f, 1f)] private float _intensity;
        [Tooltip("Room tier 1–3: tougher enemies, rarer resources and slightly better loot higher up")]
        [SerializeField, Range(1, 3)] private int _tier = 1;
        [Tooltip("Index into MapContentSettings.Factions, or -1 for none")]
        [SerializeField] private int         _faction = NoFaction;
        [SerializeField, Range(0f, 1f)] private float _factionInfluence;
        [Tooltip("Breach rooms: the second reality bleeding in (index into MapContentSettings.Factions), or -1")]
        [SerializeField] private int         _breachFaction = NoFaction;
        [Tooltip("Index into MapContentSettings.Sections, or -1 for none")]
        [SerializeField] private int         _section = NoSection;

        // For serialization: keeps the field defaults (no faction, no section) for data
        // saved before a field existed.
        private MapNode() { }

        public MapNode(int id, MapNodeType type, Vector2 position)
        {
            _id       = id;
            _type     = type;
            _position = position;
        }

        public int Id => _id;

        public MapNodeType Type
        {
            get => _type;
            set => _type = value;
        }

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        // 0 = calm, 1 = peak difficulty.
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp01(value);
        }

        // 1 = ordinary, 3 = the map's toughest rooms (see MapTierPainter).
        public int Tier
        {
            get => Mathf.Clamp(_tier, 1, 3);
            set => _tier = Mathf.Clamp(value, 1, 3);
        }

        // Graphs authored before room tiers used an Elite room for a tough fight; it counts as tier 3.
        public int EffectiveTier => _type == MapNodeType.Elite ? 3 : Tier;

        public int   Faction          => _faction;
        public float FactionInfluence => _factionInfluence;
        public bool  HasFaction       => _faction != NoFaction;

        // The other faction in a Breach room; NoFaction elsewhere.
        public int  BreachFaction    { get => _breachFaction; set => _breachFaction = value < 0 ? NoFaction : value; }
        public bool HasBreachFaction => _breachFaction != NoFaction;

        public int  Section    { get => _section; set => _section = value < 0 ? NoSection : value; }
        public bool HasSection => _section != NoSection;

        public void SetFaction(int faction, float influence)
        {
            if (faction < 0)
            {
                ClearFaction();
                return;
            }

            _faction          = faction;
            _factionInfluence = Mathf.Clamp01(influence);
        }

        public void ClearFaction()
        {
            _faction          = NoFaction;
            _factionInfluence = 0f;
        }

        public MapNode Clone() => (MapNode)MemberwiseClone();
    }
}
