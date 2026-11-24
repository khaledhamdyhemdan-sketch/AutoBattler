using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Procedural "ornate" IMGUI skin: gold filigree-style frames, gradient fills,
    // vignette backdrops and display-text helpers. All textures are generated in
    // code at runtime, so no imported UI art is required and nothing here copies
    // any reference game's assets.
    public static class OrnateGuiSkin
    {
        private static readonly Dictionary<string, Texture2D> cache =
            new Dictionary<string, Texture2D>();
        private static GUIStyle cachedButton, cachedTitle, cachedShadow;
        private static readonly Color gold = new Color(.85f, .66f, .28f);
        private static readonly Color goldBright = new Color(1f, .87f, .55f);
        private static readonly Color goldDark = new Color(.45f, .32f, .12f);
        private static readonly Color panelFill = new Color(.09f, .07f, .12f, .94f);

        public static Color Gold { get { return gold; } }
        public static Color GoldBright { get { return goldBright; } }

        // 9-slice framed panel texture: outer gold border, dark inner line,
        // translucent dark fill, small diamond accents at the corner centers.
        public static Texture2D Frame(string key, Color border, Color fill,
            int size = 64, int thickness = 5)
        {
            Texture2D found;
            if (cache.TryGetValue(key, out found)) return found;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            int half = size / 2;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int d = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                    Color c = fill;
                    if (d < thickness)
                    {
                        float t = d / (float)thickness;
                        c = Color.Lerp(goldDark, border, t);
                        if (d == thickness - 1) c = goldBright;
                    }
                    else if (d < thickness + 2) c = goldDark * .6f;
                    // corner diamonds
                    bool cornerZone = (x < half / 2 || x >= size - half / 2) &&
                        (y < half / 2 || y >= size - half / 2);
                    if (cornerZone)
                    {
                        int cx = x < half / 2 ? half / 4 : size - 1 - half / 4;
                        int cy = y < half / 2 ? half / 4 : size - 1 - half / 4;
                        if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) < 3)
                            c = goldBright;
                    }
                    px[y * size + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            cache[key] = tex;
            return found != null ? found : tex;
        }

        // Soft radial glow used behind featured hero art.
        public static Texture2D Glow(string key, Color center, int size = 128)
        {
            Texture2D found;
            if (cache.TryGetValue(key, out found)) return found;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float half = size * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) +
                        (y - half) * (y - half)) / half;
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    px[y * size + x] = new Color(center.r, center.g, center.b,
                        center.a * a);
                }
            tex.SetPixels(px);
            tex.Apply();
            cache[key] = tex;
            return found != null ? found : tex;
        }

        // Edge-darkening vignette laid over full-screen backdrops.
        public static Texture2D Vignette(int size = 128)
        {
            const string key = "vignette";
            Texture2D found;
            if (cache.TryGetValue(key, out found)) return found;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            float half = size * .5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - half) / half;
                    float dy = Mathf.Abs(y - half) / half;
                    float edge = Mathf.Max(dx, dy);
                    float a = Mathf.SmoothStep(0f, .75f, edge * edge);
                    px[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            cache[key] = tex;
            return found != null ? found : tex;
        }

        // Vertical gradient backdrop (top color to bottom color).
        public static Texture2D Backdrop(string key, Color top, Color bottom,
            int h = 64)
        {
            Texture2D found;
            if (cache.TryGetValue(key, out found)) return found;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            var px = new Color[h];
            for (int y = 0; y < h; y++)
                px[y] = Color.Lerp(bottom, top, y / (float)(h - 1));
            tex.SetPixels(px);
            tex.Apply();
            cache[key] = tex;
            return found != null ? found : tex;
        }

        public static GUIStyle ButtonStyle()
        {
            if (cachedButton != null) return cachedButton;
            var s = new GUIStyle(GUI.skin.button);
            s.normal.background = Frame("btn_n", gold,
                new Color(.16f, .11f, .07f, .96f));
            s.hover.background = Frame("btn_h", goldBright,
                new Color(.26f, .18f, .10f, .96f));
            s.active.background = Frame("btn_a", goldDark,
                new Color(.10f, .07f, .04f, .96f));
            s.border = new RectOffset(12, 12, 12, 12);
            s.normal.textColor = goldBright;
            s.hover.textColor = Color.white;
            s.active.textColor = gold;
            s.fontSize = 16;
            s.fontStyle = FontStyle.Bold;
            s.wordWrap = true;
            cachedButton = s;
            return s;
        }

        public static GUIStyle TitleStyle(int size)
        {
            if (cachedTitle != null && cachedTitle.fontSize == size) return cachedTitle;
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = goldBright;
            s.alignment = TextAnchor.MiddleCenter;
            cachedTitle = s;
            return s;
        }

        // Display text with a dark offset shadow, approximating the outlined
        // gold titles used by polished hero-collector UI.
        public static void Title(Rect r, string text, int size)
        {
            GUIStyle shadow = new GUIStyle(TitleStyle(size));
            shadow.normal.textColor = new Color(0f, 0f, 0f, .85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, shadow);
            GUI.Label(r, text, TitleStyle(size));
        }

        // Framed panel draw helper (9-slice via a bordered style).
        public static void Panel(Rect r, string key, Color border, Color fill)
        {
            var s = new GUIStyle(GUI.skin.box);
            s.normal.background = Frame(key, border, fill);
            s.border = new RectOffset(12, 12, 12, 12);
            GUI.Box(r, GUIContent.none, s);
        }

        // Standard modal backdrop: dark gradient + vignette.
        public static void ModalBackdrop(float width, float height)
        {
            GUI.DrawTexture(new Rect(0, 0, width, height),
                Backdrop("bg", new Color(.05f, .04f, .09f), new Color(.13f, .09f, .14f)));
            GUI.DrawTexture(new Rect(0, 0, width, height), Vignette());
        }
    }
}
