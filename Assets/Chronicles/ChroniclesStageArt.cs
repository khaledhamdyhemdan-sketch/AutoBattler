using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Resource-backed artwork for dialogue and stage scenes. Does not fabricate image assets.
    // Add licensed/original textures to Resources/Chronicles/Backgrounds/{origin}.png
    // and Resources/Chronicles/Portraits/{speaker_key}.png.
    public static class ChroniclesStageArt
    {
        private static readonly Dictionary<string, Texture2D> TextureCache =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        private static Texture2D Load(string path)
        {
            Texture2D image;
            if (TextureCache.TryGetValue(path, out image)) return image;
            image = Resources.Load<Texture2D>(path);
            TextureCache[path] = image;
            return image;
        }

        public static Texture2D Background(Origin origin)
        {
            return Load("Chronicles/Backgrounds/" + origin.ToString().ToLowerInvariant());
        }

        public static Texture2D Portrait(string speaker)
        {
            if (string.IsNullOrWhiteSpace(speaker) || speaker == "Narrator") return null;
            string key = speaker.Trim().ToLowerInvariant()
                .Replace(" ", "_").Replace("'", "").Replace("-", "_");
            return Load("Chronicles/Portraits/" + key);
        }

        public static void ClearCache()
        {
            TextureCache.Clear();
        }
    }
}
