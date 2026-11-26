using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using CGD.Core;
using CGD.DevTools;
using CGD.Map;

namespace CGD.Level
{
    // The room generator scene's room, set up in this component's Inspector and built on Play:
    // LevelBuilder builds the graph this makes in place of a map (RoomRecipeGraph) and starts
    // the player, or the spectator camera, inside it. The graph is the room plus one neighbour
    // room behind each doorway, each joined only to the room, so how doors and rooms meet can
    // be judged. Every choice left on Any is rolled by the seed; an empty seed rolls a new room
    // every play, and Regenerate (F5, or the Inspector's New room button) rolls another without
    // stopping.
    //
    // Runs before the LevelBuilder so the Player is already off when spectating, as if the
    // scene had been saved that way (the spectator takes over while the Player's camera is off).
    [DefaultExecutionOrder(-600)]
    public sealed class RoomGenerator : MonoBehaviour
    {
        // A dev-tool key of its own: the scene runs with the Player or the spectator, whose
        // rebindable tables should not grow an action only this scene uses.
        private const string RegenerateBinding = "<Keyboard>/f5";

        // Carries the new seed across the scene reload that Regenerate makes.
        private static string s_nextSeedText;

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
        private InputAction _regenerate;

        public MapContentSettings Content => _content;
        public RoomChoice Choice => _room;
        // The seed the room is built from: the one typed, or a fresh one for this play.
        public Seed Seed => _seed ??= _room.ResolveSeed();
        // What the room came out as once every Any was rolled, set by BuildGraph.
        public RoomRecipe Recipe { get; private set; }

        private void Awake()
        {
            if (s_nextSeedText != null)
            {
                _seed = Seed.Parse(s_nextSeedText);
                s_nextSeedText = null;
            }
            if (_playAs == RoomGeneratorView.Spectator && _player != null) _player.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _regenerate = new InputAction("Regenerate", InputActionType.Button, RegenerateBinding);
            _regenerate.performed += OnRegeneratePressed;
            _regenerate.Enable();
        }

        private void OnDisable()
        {
            _regenerate.performed -= OnRegeneratePressed;
            _regenerate.Dispose();
        }

        public MapGraph BuildGraph(LevelBuildSettings settings)
        {
            Recipe = _room.ToRecipe(Seed, settings, _content);
            return RoomRecipeGraph.Build(Recipe, Seed.Derive("neighbours").Stream());
        }

        // The layout builder for the room: its floor levels held to the ones chosen.
        public LevelLayoutBuilder LayoutBuilder(LevelBuildSettings settings)
        {
            RoomRecipe recipe = Recipe ?? _room.ToRecipe(Seed, settings, _content);
            return recipe.LayoutBuilder(settings);
        }

        // Where the room's overgrowth comes from: its factions', none, or the profile chosen.
        public OvergrowthSource Overgrowth(LevelBuildSettings settings) => _room.ToOvergrowthSource(settings);

        // Called with the room's centre once it is built: the spectator's overview starts looking at it.
        public void FrameRoom(Vector3 centre)
        {
            if (_spectatorCamera == null) return;
            if (_spectatorCamera.TryGetComponent(out SpectatorCamera spectator)) spectator.Frame(centre);
            else _spectatorCamera.position += centre;
        }

        // Builds the scene again with a new seed. The level is built in Awake, so a reload is
        // the one way to rebuild everything (geometry, NavMesh, enemies) from scratch.
        public void Regenerate()
        {
            s_nextSeedText = RoomChoice.NewSeedText();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void OnRegeneratePressed(InputAction.CallbackContext _) => Regenerate();
    }
}
