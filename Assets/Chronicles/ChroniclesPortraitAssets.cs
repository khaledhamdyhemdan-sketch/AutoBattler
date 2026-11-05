using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Cached production-art lookup for hero portraits.
    /// Static: Assets/Resources/Chronicles/Portraits/Static/{heroId}.png
    /// Animation: Assets/Resources/Chronicles/Portraits/Animated/{heroId}/frame_000.png ...
    /// Files are authored art, not generated or copied from third-party games.
    /// When frames are missing, the renderer uses the still + subtle motion overlay.
    /// When both are missing, it uses the existing distinct procedural development art.
    /// </summary>
    public static class ChroniclesPortraitAssets
    {
        private sealed class Entry
        {
            public Texture2D still;
            public Texture2D[] frames;
            public bool staticLoaded, framesLoaded;
        }

        private static readonly Dictionary<string, Entry> Cache =
            new Dictionary<string, Entry>(StringComparer.Ordinal);
        private const int MaxCachedAnimatedHeroes = 2;
        private static readonly LinkedList<string> RecentAnimatedHeroes =
            new LinkedList<string>();

        private static Entry Resolve(string id)
        {
            if (string.IsNullOrEmpty(id) || WorldsCatalog.FindHero(id) == null)
                return null;
            Entry entry;
            if (!Cache.TryGetValue(id, out entry))
            {
                entry = new Entry();
                Cache.Add(id, entry);
            }
            return entry;
        }

        public static Texture2D Static(string heroId)
        {
            Entry entry = Resolve(heroId);
            if (entry == null) return null;
            if (!entry.staticLoaded)
            {
                entry.staticLoaded = true;
                entry.still = Resources.Load<Texture2D>(
                    "Chronicles/Portraits/Static/" + heroId);
                if (entry.still == null)
                    entry.still = ChroniclesStageArt.HeroCard(heroId);
            }
            return entry.still;
        }

        public static Texture2D[] AnimatedFrames(string heroId)
        {
            Entry entry = Resolve(heroId);
            if (entry == null) return new Texture2D[0];
            if (!entry.framesLoaded)
            {
                entry.framesLoaded = true;
                Texture2D[] found = Resources.LoadAll<Texture2D>(
                    "Chronicles/Portraits/Animated/" + heroId);
                Array.Sort(found, (left, right) =>
                    string.CompareOrdinal(left.name, right.name));
                entry.frames = found;
            }
            if (entry.frames.Length > 0)
            {
                RecentAnimatedHeroes.Remove(heroId);
                RecentAnimatedHeroes.AddLast(heroId);
                TrimAnimatedCache();
            }
            return entry.frames;
        }

        private static void TrimAnimatedCache()
        {
            while (RecentAnimatedHeroes.Count > MaxCachedAnimatedHeroes)
            {
                string expiredId = RecentAnimatedHeroes.First.Value;
                RecentAnimatedHeroes.RemoveFirst();
                Entry expired;
                if (!Cache.TryGetValue(expiredId, out expired) ||
                    expired.frames == null) continue;
                foreach (Texture2D frame in expired.frames)
                    if (frame != null) Resources.UnloadAsset(frame);
                expired.frames = null;
                expired.framesLoaded = false;
            }
        }

        public static bool HasAuthoredStatic(string heroId)
        {
            Entry entry = Resolve(heroId);
            if (entry == null) return false;
            // The fallback card art also counts as manually supplied art.
            return Static(heroId) != null;
        }

        public static bool HasAuthoredAnimation(string heroId)
        {
            return AnimatedFrames(heroId).Length > 1;
        }

        public static void Clear()
        {
            // Resources.Load assets are Unity-owned. Do not Destroy them.
            Cache.Clear();
            RecentAnimatedHeroes.Clear();
        }
    }
}
