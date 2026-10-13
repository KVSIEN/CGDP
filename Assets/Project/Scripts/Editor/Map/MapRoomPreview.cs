using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Editor
{
    // The room each node of the open graph would become in a level — its place (lobby,
    // casino…), cells and floor plan — for labelling nodes. Runs only the room pass of the
    // level layout (LevelLayoutBuilder.PlaceRooms) with the graph's seed and the session's
    // build settings, as LevelBuilder does, and only again after an edit or a new seed.
    public class MapRoomPreview
    {
        private LevelLayout        _layout;
        private LevelBuildSettings _builtWith;
        private int                _builtVersion = -1;
        private Seed               _builtSeed;

        public bool TryGetRoom(MapGraphEditorSession session, int nodeId, out LevelRoom room)
        {
            Refresh(session);
            room = null;
            return _layout != null && _layout.Rooms.TryGetValue(nodeId, out room);
        }

        private void Refresh(MapGraphEditorSession session)
        {
            LevelBuildSettings settings = session.BuildSettings;
            if (settings == null)
            {
                _layout = null;
                return;
            }

            MapGraphAsset asset = session.Asset;
            if (_layout != null && _builtWith == settings && _builtVersion == session.Version && _builtSeed == asset.Seed) return;

            _builtWith    = settings;
            _builtVersion = session.Version;
            _builtSeed    = asset.Seed;
            _layout = new LevelLayoutBuilder(settings).PlaceRooms(session.Graph, asset.NodeSpacing, asset.Seed, asset.Content);
        }
    }
}
