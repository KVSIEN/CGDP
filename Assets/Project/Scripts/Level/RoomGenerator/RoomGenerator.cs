using UnityEngine;
using UnityEngine.SceneManagement;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // The room generator scene's room: LevelBuilder builds the one-room graph this makes in
    // place of a map (RoomRecipeGraph) and puts the player inside it. What the room is comes
    // from the RoomChoice the panel last saved; Apply saves a new one and reloads the scene,
    // so the room is built from scratch exactly as a level builds it.
    public sealed class RoomGenerator : MonoBehaviour
    {
        private const string SaveKey = "room_generator_choice";

        [Tooltip("Where the factions and ship sections to choose from come from")]
        [SerializeField] private MapContentSettings _content;

        private RoomChoice _choice;

        public MapContentSettings Content => _content;
        // The build settings the room was built with (the level builder's), set by BuildGraph.
        public LevelBuildSettings Settings { get; private set; }
        // What the room came out as once every Random was rolled.
        public RoomRecipe Recipe { get; private set; }

        public RoomChoice Choice => _choice ??= Load();
        public Seed Seed => Seed.Parse(Choice.SeedText);

        public MapGraph BuildGraph(LevelBuildSettings settings)
        {
            Settings = settings;
            Recipe = Choice.ToRecipe(settings, _content);
            return RoomRecipeGraph.Build(Recipe);
        }

        // Builds the room again from `choice`; the scene reloads, so nothing of the old room stays.
        public void Apply(RoomChoice choice)
        {
            _choice = choice;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(choice));
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static RoomChoice Load()
        {
            var choice = new RoomChoice { SeedText = RoomChoice.NewSeedText() };
            if (PlayerPrefs.HasKey(SaveKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(SaveKey), choice);
            return choice;
        }
    }
}
