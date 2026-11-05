using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Original procedural UR card ornament: myth-specific geometry, layered
    /// Light/Dark palettes, soft motion and hero-specific variations.
    /// This deliberately supplements the placeholder portrait; it is NOT
    /// the approved final 2D artwork or a replacement for original rigs.
    /// </summary>
    public static class ChroniclesPremiumFrame
    {
        public static bool Supports(string heroId)
        {
            HeroDefinition hero = WorldsCatalog.FindHero(heroId);
            return hero != null && hero.rarity == Rarity.UR &&
                URCardCatalog.For(heroId) != null;
        }

        public static void Draw(Rect frame, string heroId, bool animated)
        {
            HeroDefinition hero = WorldsCatalog.FindHero(heroId);
            URCardIdentity art = URCardCatalog.For(heroId);
            if (hero == null || art == null || hero.rarity != Rarity.UR) return;

            float w = frame.width, h = frame.height;
            if (w < 15f || h < 15f) return;
            Color original = GUI.color;
            bool dark = hero.element == HeroElement.Darkness;
            Color primary = art.border;
            Color secondary = art.secondary;
            float t = animated ? Time.unscaledTime : 0f;
            float pulse = animated ? (.80f + .20f * Mathf.Sin(t * .9f +
                HeroSeed(heroId) * .1f)) : .92f;

            // Thin ceremonial outer and inner insets. Alpha is intentionally
            // low to avoid overpowering faces in small hero thumbnails.
            Stroke(new Rect(0f, 0f, w, h), dark ?
                Color.Lerp(primary, Color.black, .28f) : primary, 4f);
            Stroke(new Rect(5f, 5f, w - 10f, h - 10f),
                Alpha(secondary, .65f * pulse), 1.5f);
            Stroke(new Rect(9f, 9f, w - 18f, h - 18f),
                Alpha(primary, .32f), 1f);

            // Unique personal sigil points and sun/starlight energy particles.
            int seed = HeroSeed(heroId);
            for (int i = 0; i < 7; i++)
            {
                float spot = Mathf.Repeat((seed % 17) * .031f +
                    i * .165f + (animated ? t * .018f : 0f), 1f);
                float x = 7f + (w - 14f) * spot;
                float size = (i % 3 == 0 ? 4f : 2f) * pulse;
                Fill(new Rect(x - size * .5f, 2f, size, size),
                    Alpha(secondary, i % 3 == 0 ? .8f : .47f));
            }

            float motif = Mathf.Min(w, h) * .11f;
            motif = Mathf.Clamp(motif, 6f, 29f);
            DrawCorner(hero.origin, new Vector2(motif * .25f, motif * .25f),
                motif, primary, secondary, seed);
            DrawCorner(hero.origin, new Vector2(w - motif * 1.25f, motif * .25f),
                motif, primary, secondary, seed + 1);
            DrawCorner(hero.origin, new Vector2(motif * .25f, h - motif * 1.25f),
                motif, primary, secondary, seed + 2);
            DrawCorner(hero.origin,
                new Vector2(w - motif * 1.25f, h - motif * 1.25f),
                motif, primary, secondary, seed + 3);

            // Cosmic Light = sky rays and a luminous cap; Darkness = a
            // shadow-gate underside. The motifs remain attached to the
            // authentic mythology chosen for each original fantasy hero.
            if (dark)
            {
                Fill(new Rect(w * .3f, h - 4f, w * .4f, 3f),
                    Alpha(secondary, .75f * pulse));
                Fill(new Rect(w * .48f, h - 9f, w * .04f, 9f),
                    Alpha(primary, .8f));
            }
            else
            {
                Fill(new Rect(w * .33f, 0f, w * .34f, 3f),
                    Alpha(secondary, .85f * pulse));
                Fill(new Rect(w * .48f, 0f, w * .04f, 9f),
                    Alpha(primary, .8f));
            }
            GUI.color = original;
        }

        private static void DrawCorner(Origin origin, Vector2 at, float m,
            Color primary, Color secondary, int seed)
        {
            float x = at.x, y = at.y;
            float unit = Mathf.Max(1.2f, m / 6f);
            switch (origin)
            {
                case Origin.Egypt:
                    // Solar-barge / stepped lotus geometries.
                    Fill(new Rect(x, y + m * .48f, m, unit),
                        Alpha(primary, .86f));
                    Fill(new Rect(x + m * .47f, y, unit, m),
                        Alpha(secondary, .78f));
                    Fill(new Rect(x + m * .25f, y + m * .25f,
                        unit * 2f, unit * 2f), Alpha(primary, .88f));
                    break;
                case Origin.Greece:
                    // Interlocking Hellenic key / column.
                    Fill(new Rect(x, y, m, unit), Alpha(primary, .86f));
                    Fill(new Rect(x, y, unit, m), Alpha(secondary, .7f));
                    Fill(new Rect(x + m * .40f, y + m * .35f,
                        m * .4f, unit), Alpha(primary, .75f));
                    Fill(new Rect(x + m * .40f, y + m * .35f,
                        unit, m * .45f), Alpha(secondary, .72f));
                    break;
                case Origin.Norse:
                    // Angular rune splinters and Yggdrasil branch.
                    Fill(new Rect(x, y + m * .2f, unit, m * .7f),
                        Alpha(primary, .9f));
                    Fill(new Rect(x + m * .35f, y + m * .25f,
                        unit, m * .62f), Alpha(secondary, .86f));
                    Fill(new Rect(x, y + m * .42f, m * .8f, unit),
                        Alpha(primary, .56f));
                    break;
                case Origin.Japan:
                    // Lacquered gate motif, without using actual religious icons.
                    Fill(new Rect(x, y + m * .2f, m, unit * 1.3f),
                        Alpha(primary, .92f));
                    Fill(new Rect(x + m * .20f, y + m * .22f,
                        unit, m * .7f), Alpha(secondary, .77f));
                    Fill(new Rect(x + m * .72f, y + m * .22f,
                        unit, m * .7f), Alpha(secondary, .77f));
                    break;
                case Origin.China:
                    // Layered jade clouds and sky-repairing stone.
                    Fill(new Rect(x, y + m * .35f, m * .65f, unit),
                        Alpha(primary, .86f));
                    Fill(new Rect(x + m * .27f, y + m * .16f,
                        m * .58f, unit), Alpha(secondary, .74f));
                    Fill(new Rect(x + m * .42f, y + m * .56f,
                        m * .44f, unit), Alpha(primary, .7f));
                    break;
                default:
                    Fill(new Rect(x, y + m * .5f, m, unit),
                        Alpha(primary, .8f));
                    break;
            }
            float marker = seed % 3 == 0 ? unit * 2f : unit;
            Fill(new Rect(x + m * .5f - marker * .5f,
                y + m * .5f - marker * .5f, marker, marker),
                Alpha(secondary, .8f));
        }

        private static void Stroke(Rect r, Color color, float thickness)
        {
            if (r.width <= 0f || r.height <= 0f) return;
            Fill(new Rect(r.x, r.y, r.width, thickness), color);
            Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
            Fill(new Rect(r.x, r.y, thickness, r.height), color);
            Fill(new Rect(r.xMax - thickness, r.y, thickness, r.height), color);
        }

        private static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static int HeroSeed(string id)
        {
            unchecked
            {
                int hash = 17;
                if (id != null)
                    foreach (char c in id) hash = hash * 31 + c;
                return hash & 0x7fffffff;
            }
        }
    }
}
