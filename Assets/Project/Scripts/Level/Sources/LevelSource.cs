using System;
using System.Collections.Generic;
using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // Where LevelBuilder's map graph comes from, and what each kind decides about the level built
    // from it: a saved graph, a map generated on the spot, the room gallery, or the room
    // generator's one room. LevelBuilder picks one in Awake from what its Inspector was given.
    internal abstract class LevelSource
    {
        // The seed to build with: the one typed, or with none (empty, or "0") this kind's own.
        public virtual Seed ResolveSeed(string typed) => RollsFresh(typed) ? OwnSeed() : Seed.Parse(typed);

        // The graph to build, or null when there is none; Modifiers is known after.
        public abstract MapGraph BuildGraph(LevelBuildSettings settings, Seed seed);

        // What a graph position is worth in layout cells.
        public virtual Vector2 NodeSpacing => Vector2.one;
        // Where the graph's faction indices point (null = rooms have no faction).
        public abstract MapContentSettings Content { get; }
        // The run modifiers the map was made with.
        public virtual IReadOnlyList<MapRunModifier> Modifiers => Array.Empty<MapRunModifier>();
        // A walk-through of every place: rooms are announced every time, with their side and depth.
        public virtual bool IsGallery => false;

        public virtual LevelLayoutBuilder LayoutBuilder(LevelBuildSettings settings) => new(settings);
        public virtual OvergrowthSource Overgrowth(LevelBuildSettings settings) => OvergrowthSource.FromFactions(settings);
        // The room the player starts in: `pinned` (LevelBuilder's Start Room; -1 = the Start room's).
        public virtual int StartRoom(int pinned) => pinned;
        // Told the start room's centre once the level is built, and how far from it the level reaches.
        public virtual void Started(Vector3 centre, float reach) { }

        protected virtual Seed OwnSeed() => Seed.Random();

        // "0" too: the field used to be a number where 0 meant a fresh seed, and scenes saved then still hold it.
        private static bool RollsFresh(string text) => string.IsNullOrWhiteSpace(text) || text.Trim() == "0";
    }
}
