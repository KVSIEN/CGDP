using System.Collections.Generic;
using CGD.Core;
using CGD.Level;
using CGD.Map;

namespace CGD.Editor
{
    // A layout built over a run of seeds through the level's own pipeline (RoomSampleBuilder),
    // to see at a glance whether it holds up: does every room build, do the place's Required
    // pieces always go down, does each set piece and set fit, is any floor cut off. What
    // RoomLayoutTests asserts, as a readout a designer gets while editing the layout.
    public sealed class RoomLayoutCheck
    {
        public const int DefaultSeeds = 12;
        private const string CutOff = "can't be walked to";
        // Where a piece comes from; a set's pieces go by the set's name.
        private const string AnchorSource = "Anchor";
        private const string PieceSource  = "Set piece";
        private const string PlaceSource  = "Place";

        private readonly Dictionary<RoomPropPlacement, RoomLayoutCheckPiece> _byPlacement = new();
        private readonly List<RoomLayoutCheckPiece> _pieces = new();

        private RoomLayoutCheck(RoomFunction place, RoomLayout layout)
        {
            Place  = place;
            Layout = layout;
        }

        public RoomFunction Place  { get; }
        public RoomLayout   Layout { get; }
        public int Built { get; private set; }
        public int Failed { get; private set; }
        public int RoomsCutOff { get; private set; }
        // The first reason a room failed to build.
        public string FirstFailure { get; private set; }
        public IReadOnlyList<RoomLayoutCheckPiece> Pieces => _pieces;

        // Rooms that lost a Required piece; a sound layout has none.
        public int RoomsMissingRequired { get; private set; }

        public string Summary
        {
            get
            {
                if (Built == 0) return $"No room built: {FirstFailure}";
                var notes = new List<string>();
                if (Failed > 0) notes.Add($"{Failed} failed");
                if (RoomsMissingRequired > 0) notes.Add($"{RoomsMissingRequired} missing a required piece");
                if (RoomsCutOff > 0) notes.Add($"{RoomsCutOff} with floor cut off");
                RoomLayoutCheckPiece weakest = Weakest;
                if (weakest != null && weakest.FitRate < 0.5f) notes.Add($"{weakest.Name} fits in {weakest.FitRate:P0}");
                return notes.Count == 0 ? $"Sound over {Built} rooms" : $"{Built} rooms: {string.Join(", ", notes)}";
            }
        }

        public bool IsSound => Built > 0 && Failed == 0 && RoomsMissingRequired == 0 && RoomsCutOff == 0
                               && (Weakest == null || Weakest.FitRate >= 0.5f);

        // The layout's own set piece or set that fits least often (the place's pieces aside, and
        // pieces only for bigger rooms than any built).
        private RoomLayoutCheckPiece Weakest
        {
            get
            {
                RoomLayoutCheckPiece weakest = null;
                foreach (RoomLayoutCheckPiece piece in _pieces)
                    if (piece.Rooms > 0 && piece.Source != PlaceSource && piece.Source != AnchorSource
                        && (weakest == null || piece.FitRate < weakest.FitRate))
                        weakest = piece;
                return weakest;
            }
        }

        // `layout` null checks the place on its own.
        public static RoomLayoutCheck Run(LevelBuildSettings settings, RoomFunction place, RoomLayout layout, int seeds = DefaultSeeds)
        {
            var check = new RoomLayoutCheck(place, layout);
            check.Index();

            var recipe = new RoomLabRecipe
            {
                BuildSettings = settings,
                Room          = new RoomRecipe { Type = MapNodeType.Quiet, Place = place, Layout = layout, Category = place.Category },
            };
            foreach (string seed in SeedSequence.CountingFrom("1", seeds)) check.Add(Build(recipe, seed));
            return check;
        }

        private static RoomSample Build(RoomLabRecipe recipe, string seed)
        {
            try
            {
                return RoomSampleBuilder.Build(recipe, seed);
            }
            catch (System.Exception exception)
            {
                // A room the pipeline can't build counts as failed; the Room Lab shows it in full.
                return new RoomSample($"seed {seed}: {exception.Message}");
            }
        }

        // Every placement the room can get, in the order FunctionProps hands them over.
        private void Index()
        {
            foreach (RoomPropPlacement placement in Place.Props)
                if (placement != null && placement.Required) Track(placement, AnchorSource);
            if (Layout != null)
            {
                foreach (RoomPropPlacement placement in Layout.Props) Track(placement, PieceSource);
                foreach (RoomPropSet set in Layout.Sets) TrackSet(set);
            }
            foreach (RoomPropPlacement placement in Place.Props)
                if (placement != null && !placement.Required) Track(placement, PlaceSource);
            foreach (RoomPropSet set in Place.Sets) TrackSet(set);
        }

        private void TrackSet(RoomPropSet set)
        {
            if (set == null) return;
            foreach (RoomPropPlacement placement in set.Props) Track(placement, set.DisplayName);
        }

        private void Track(RoomPropPlacement placement, string source)
        {
            if (placement == null || _byPlacement.ContainsKey(placement)) return;
            var piece = new RoomLayoutCheckPiece(placement, source);
            _byPlacement.Add(placement, piece);
            _pieces.Add(piece);
        }

        private void Add(RoomSample sample)
        {
            if (!sample.IsValid)
            {
                Failed++;
                FirstFailure ??= sample.Message;
                return;
            }
            Built++;

            bool missing = false;
            foreach (PropPlacementOutcome outcome in sample.Furnishing.Outcomes)
            {
                if (outcome.Placement == null || !_byPlacement.TryGetValue(outcome.Placement, out RoomLayoutCheckPiece piece)) continue;
                piece.Add(outcome);
                if (outcome.Placement.Required && outcome.Placed == 0) missing = true;
            }
            if (missing) RoomsMissingRequired++;

            foreach (RoomIssue issue in sample.Issues)
                if (issue.Text.Contains(CutOff))
                {
                    RoomsCutOff++;
                    break;
                }
        }
    }
}
