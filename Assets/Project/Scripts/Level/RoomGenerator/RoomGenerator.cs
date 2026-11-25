using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // The room generator scene's room, set up in this component's Inspector and built on Play:
    // LevelBuilder builds the one-room graph this makes in place of a map (RoomRecipeGraph)
    // and starts the player, or the spectator camera, inside it. Every choice left on Any is
    // rolled by the seed; an empty seed rolls a new room every play.
    //
    // Runs before the LevelBuilder so the Player is already off when spectating, as if the
    // scene had been saved that way (the spectator takes over while the Player's camera is off).
    [DefaultExecutionOrder(-600)]
    public sealed class RoomGenerator : MonoBehaviour
    {
        [Tooltip("Stand in the room as the player, or watch it with the spectator camera (the Player is switched off)")]
        [SerializeField] private RoomGeneratorView _playAs = RoomGeneratorView.Player;
        [SerializeField] private RoomChoice _room = new();

        [Header("Scene")]
        [Tooltip("Where the factions and ship sections to choose from come from")]
        [SerializeField] private MapContentSettings _content;
        [Tooltip("Switched off when spectating")]
        [SerializeField] private Transform _player;
        [Tooltip("Optional — the spectator camera, moved over the room keeping its placed offset from the origin")]
        [SerializeField] private Transform _spectatorCamera;

        private Seed? _seed;

        public MapContentSettings Content => _content;
        public RoomChoice Choice => _room;
        // The seed the room is built from: the one typed, or a fresh one for this play.
        public Seed Seed => _seed ??= _room.ResolveSeed();
        // What the room came out as once every Any was rolled, set by BuildGraph.
        public RoomRecipe Recipe { get; private set; }

        private void Awake()
        {
            if (_playAs == RoomGeneratorView.Spectator && _player != null) _player.gameObject.SetActive(false);
        }

        public MapGraph BuildGraph(LevelBuildSettings settings)
        {
            Recipe = _room.ToRecipe(Seed, settings, _content);
            return RoomRecipeGraph.Build(Recipe);
        }

        // The layout builder for the room: its floor levels held to the ones chosen.
        public LevelLayoutBuilder LayoutBuilder(LevelBuildSettings settings)
        {
            RoomRecipe recipe = Recipe ?? _room.ToRecipe(Seed, settings, _content);
            return new LevelLayoutBuilder(settings, recipe.LowestLevel, recipe.HighestLevel);
        }

        // Called with the room's centre once it is built; the spectator's overview looks at it.
        public void FrameRoom(Vector3 centre)
        {
            if (_spectatorCamera != null) _spectatorCamera.position += centre;
        }
    }
}
