using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // A walk-through of every place, faction and side (the RoomGallery scene). Positions are in cells.
    internal sealed class GalleryLevelSource : LevelSource
    {
        private readonly RoomGallerySettings _gallery;

        public GalleryLevelSource(RoomGallerySettings gallery) => _gallery = gallery;

        public override MapGraph BuildGraph(LevelBuildSettings settings, Seed seed) => RoomGalleryGraph.Build(_gallery, settings);
        public override MapContentSettings Content => _gallery.Content;
        public override bool IsGallery => true;
    }
}
