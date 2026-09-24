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

        [SerializeField] private int         _id;
        [SerializeField] private MapNodeType _type;
        [Tooltip("Editor/debug position only — not room geometry")]
        [SerializeField] private Vector2     _position;
        [SerializeField, Range(0f, 1f)] private float _intensity;
        [Tooltip("Index into MapGenerationSettings.Factions, or -1 for none")]
        [SerializeField] private int         _faction = NoFaction;
        [SerializeField, Range(0f, 1f)] private float _factionInfluence;

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

        public int   Faction          => _faction;
        public float FactionInfluence => _factionInfluence;
        public bool  HasFaction       => _faction != NoFaction;

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
