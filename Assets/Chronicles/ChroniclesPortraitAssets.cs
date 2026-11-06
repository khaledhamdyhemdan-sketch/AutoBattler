using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Cached production-art lookup for hero portraits.
    /// Static: Assets/Resources/Chronicles/Portraits/Static/{heroId}.png
    /// Animation: Assets/Resources/Chronicles/Portraits/Animated/{heroId}/frame_000.png ...
    /// Skin static: Assets/Resources/Chronicles/Portraits/Skins/{heroId}/{skinId}.png
    /// Skin animation: Assets/Resources/Chronicles/Portraits/SkinAnimated/{heroId}/{skinId}/frame_000.png ...
    /// The equipped skin (via EquippedSkinResolver) is used when its art exists; a skin
    /// with only a still never borrows the base frames (that would show the wrong costume).
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
        public const string DefaultSkin = "default";

        /// <summary>Returns the equipped skin ID for a hero (set by the game at startup).</summary>
        public static Func<string, string> EquippedSkinResolver;

        private static string Key(string heroId, string skinId)
        {
            return skinId == DefaultSkin ? heroId : heroId + "|" + skinId;
        }

        private static string Equipped(string heroId)
        {
            string skin = EquippedSkinResolver != null ? EquippedSkinResolver(heroId) : null;
            return string.IsNullOrEmpty(skin) ? DefaultSkin : skin;
        }

        private static Entry Resolve(string id, string skinId)
        {
            if (string.IsNullOrEmpty(id) || WorldsCatalog.FindHero(id) == null)
                return null;
            if (skinId != DefaultSkin && HeroSkinCatalog.Find(id, skinId) == null)
                return null;
            Entry entry;
            string key = Key(id, skinId);
            if (!Cache.TryGetValue(key, out entry))
            {
                entry = new Entry();
                Cache.Add(key, entry);
            }
            return entry;
        }

        /// <summary>Authored still for a specific skin, or null when that skin has no art.</summary>
        public static Texture2D SkinStatic(string heroId, string skinId)
        {
            if (string.IsNullOrEmpty(skinId) || skinId == DefaultSkin) return null;
            Entry entry = Resolve(heroId, skinId);
            if (entry == null) return null;
            if (!entry.staticLoaded)
            {
                entry.staticLoaded = true;
                entry.still = Resources.Load<Texture2D>(
                    "Chronicles/Portraits/Skins/" + heroId + "/" + skinId);
            }
            return entry.still;
        }

        public static bool HasSkinArt(string heroId, string skinId)
        {
            return SkinStatic(heroId, skinId) != null;
        }

        /// <summary>Equipped-skin still when authored, otherwise the base portrait.</summary>
        public static Texture2D Static(string heroId)
        {
            return Static(heroId, Equipped(heroId));
        }

        public static Texture2D Static(string heroId, string skinId)
        {
            Texture2D skinned = SkinStatic(heroId, skinId);
            if (skinned != null) return skinned;
            Entry entry = Resolve(heroId, DefaultSkin);
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
            return AnimatedFrames(heroId, Equipped(heroId));
        }

        public static Texture2D[] AnimatedFrames(string heroId, string skinId)
        {
            bool skinned = SkinStatic(heroId, skinId) != null;
            if (!skinned) skinId = DefaultSkin;
            Entry entry = Resolve(heroId, skinId);
            if (entry == null) return new Texture2D[0];
            string key = Key(heroId, skinId);
            if (!entry.framesLoaded)
            {
                entry.framesLoaded = true;
                Texture2D[] found = Resources.LoadAll<Texture2D>(skinned ?
                    "Chronicles/Portraits/SkinAnimated/" + heroId + "/" + skinId :
                    "Chronicles/Portraits/Animated/" + heroId);
                Array.Sort(found, (left, right) =>
                    string.CompareOrdinal(left.name, right.name));
                entry.frames = found;
            }
            if (entry.frames.Length > 0)
            {
                RecentAnimatedHeroes.Remove(key);
                RecentAnimatedHeroes.AddLast(key);
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
            Entry entry = Resolve(heroId, DefaultSkin);
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
