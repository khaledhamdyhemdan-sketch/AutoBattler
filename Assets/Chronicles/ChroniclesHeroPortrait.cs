using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Generates distinct lightweight 2D placeholder illustrations and a
    /// parallax/breathing/particle animated portrait for all 41 catalog heroes.
    /// If production Texture2D portraits / frame sequences are imported later,
    /// authored assets override procedural artwork without code changes.
    /// These development illustrations are NOT complete anime character art.
    /// </summary>
    public static class ChroniclesHeroPortrait
    {
        private const int Width = 256;
        private const int Height = 384;

        private sealed class Layers
        {
            public Texture2D background;
            public Texture2D character;
            public Texture2D still;
        }
        private static readonly Dictionary<string, Layers> Cache =
            new Dictionary<string, Layers>(StringComparer.Ordinal);

        public static bool HasIdentity(string heroId)
        {
            return HeroPortraitIdentityCatalog.For(heroId) != null;
        }

        public static void Clear()
        {
            foreach (Layers entry in Cache.Values)
            {
                if (entry.background != null) UnityEngine.Object.Destroy(entry.background);
                if (entry.character != null) UnityEngine.Object.Destroy(entry.character);
                if (entry.still != null) UnityEngine.Object.Destroy(entry.still);
            }
            Cache.Clear();
            ChroniclesPortraitAssets.Clear();
        }


        public static void Draw(string heroId, Rect frame, bool animated, float phase = 0f)
        {
            PortraitIdentity profile = HeroPortraitIdentityCatalog.For(heroId);
            if (profile == null) return;

            GUI.BeginGroup(frame);
            Rect local = new Rect(0, 0, frame.width, frame.height);
            Texture2D authoredStill = ChroniclesPortraitAssets.Static(heroId);
            Texture2D[] frames = animated ?
                ChroniclesPortraitAssets.AnimatedFrames(heroId) : null;
            if (frames != null && frames.Length > 0)
            {
                int index = Mathf.FloorToInt(Time.unscaledTime * 8f + phase * 3f) % frames.Length;
                GUI.DrawTexture(local, frames[Mathf.Abs(index)], ScaleMode.ScaleAndCrop);
            }
            else if (authoredStill != null)
            {
                GUI.DrawTexture(local, authoredStill, ScaleMode.ScaleAndCrop);
                // A still illustration can animate as a gentle motion portrait
                // until a full frame animation / rig is authored.
                if (animated) AnimatedLightOverlay(heroId, local, phase, profile);
            }
            else
            {
                Layers layers = GetOrCreate(profile);
                GUI.DrawTexture(local, layers.background, ScaleMode.StretchToFill);
                if (!animated)
                {
                    GUI.DrawTexture(local, layers.character, ScaleMode.StretchToFill);
                }
                else
                {
                    float clock = Time.unscaledTime + phase;
                    float breathe = 1f + Mathf.Sin(clock * 1.65f) * .014f;
                    float moveY = Mathf.Sin(clock * 1.35f) * frame.height * .013f;
                    float x = -frame.width * (breathe - 1f) * .5f;
                    float y = -frame.height * (breathe - 1f) * .5f + moveY;
                    Rect characterRect = new Rect(x, y,
                        frame.width * breathe, frame.height * breathe);
                    GUI.DrawTexture(characterRect, layers.character, ScaleMode.StretchToFill);
                    AnimatedLightOverlay(heroId, local, phase, profile);
                }
            }
            DrawRarityBorder(local, heroId, animated);
            GUI.EndGroup();
        }

        private static void AnimatedLightOverlay(string id, Rect frame, float phase, PortraitIdentity profile)
        {
            Color old = GUI.color;
            float t = Time.unscaledTime + phase;
            HeroDefinition hero = WorldsCatalog.FindHero(id);
            bool ur = hero != null && hero.rarity == Rarity.UR;
            int count = ur ? 8 : 5;
            for (int i = 0; i < count; i++)
            {
                float seed = Hash(i * 127 + id.Length * 19) * 10f;
                float x = frame.width * (.1f + Hash(i * 13 + id.Length * 37) * .8f);
                float y = Mathf.Repeat(
                    (seed * .2f + t * (ur ? .11f : .07f)) * frame.height,
                    frame.height + 35f) - 15f;
                float pulse = .4f + .6f * (1f + Mathf.Sin(t * 2f + seed)) * .5f;
                Color color = profile.highlight;
                color.a = (ur ? .48f : .24f) * pulse;
                GUI.color = color;
                float size = ur ? 4f + (i % 3) * 1.8f : 3f + i % 2;
                GUI.DrawTexture(new Rect(x, frame.height - y, size, size),
                    Texture2D.whiteTexture);
            }
            if (ur)
            {
                // Soft pulsing edge light; no strobing or full-screen flashes.
                Color shine = profile.highlight;
                shine.a = .15f + .10f * (1f + Mathf.Sin(t * 1.2f)) * .5f;
                GUI.color = shine;
                GUI.DrawTexture(new Rect(1, 0, 3, frame.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(frame.width - 4, 0, 3, frame.height),
                    Texture2D.whiteTexture);
            }
            GUI.color = old;
        }

        private static void DrawRarityBorder(Rect r, string id, bool animated)
        {
            HeroDefinition hero = WorldsCatalog.FindHero(id);
            if (hero == null) return;
            if (hero.rarity == Rarity.UR && ChroniclesPremiumFrame.Supports(id))
            {
                ChroniclesPremiumFrame.Draw(r, id, animated);
                return;
            }
            Color color = hero.rarity == Rarity.UR ?
                (hero.element == HeroElement.Light ? new Color(1f, .83f, .42f) :
                    new Color(.70f, .45f, 1f)) :
                hero.rarity == Rarity.SSR ? new Color(.63f, .60f, .93f) :
                hero.rarity == Rarity.SR ? new Color(.45f, .67f, .88f) :
                new Color(.58f, .62f, .64f);
            Color old = GUI.color;
            GUI.color = color;
            float border = hero.rarity == Rarity.UR ? 3f : 2f;
            GUI.DrawTexture(new Rect(0, 0, r.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, r.height - border, r.width, border), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, border, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.width - border, 0, border, r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static Layers GetOrCreate(PortraitIdentity profile)
        {
            Layers entry;
            if (Cache.TryGetValue(profile.heroId, out entry)) return entry;
            HeroDefinition hero = WorldsCatalog.FindHero(profile.heroId);
            var background = new Painter(Width, Height);
            var figure = new Painter(Width, Height);
            DrawBackground(background, profile, hero);
            DrawFigure(figure, profile, hero);
            entry = new Layers
            {
                background = background.ToTexture(profile.heroId + "_generated_background"),
                character = figure.ToTexture(profile.heroId + "_generated_character")
            };
            Cache[profile.heroId] = entry;
            return entry;
        }

        private static float Hash(int seed)
        {
            unchecked
            {
                uint x = (uint)seed * 747796405u + 2891336453u;
                x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
                x = (x >> 22) ^ x;
                return (x & 0xFFFFFFu) / 16777215f;
            }
        }

        private static void DrawBackground(Painter p, PortraitIdentity style, HeroDefinition hero)
        {
            Color tint = style.highlight;
            bool light = hero != null && hero.element == HeroElement.Light;
            bool dark = hero != null && hero.element == HeroElement.Darkness;
            Color bottom = dark ? new Color(.055f, .026f, .10f) :
                light ? new Color(.22f, .16f, .11f) : new Color(.060f, .082f, .17f);
            Color top = Color.Lerp(bottom, tint, dark ? .26f : light ? .35f : .25f);
            for (int y = 0; y < Height; y++)
            {
                float blend = Mathf.Pow(y / (float)(Height - 1), 1.3f);
                Color shade = Color.Lerp(bottom, top, blend);
                float vignette = .65f + .35f * Mathf.Sin(blend * 2f);
                for (int x = 0; x < Width; x++)
                {
                    float horizon = 1f - Mathf.Abs(x - Width*.5f) / (Width*.7f);
                    p.Put(x,y,shade * (vignette * Mathf.Lerp(.78f,1.13f,horizon)),false);
                }
            }
            p.Glow(125, 275, 125, Color.Lerp(tint, Color.white, .25f), dark ? .32f : .40f);
            for(int i=0;i<80;i++)
            {
                int x=(int)(Hash(i*419+style.heroId.Length*43)*Width);
                int y=(int)(Hash(i*967+style.heroId.Length*77)*Height);
                int size=i%7==0?2:1;
                Color dot=Color.Lerp(style.highlight,Color.white,Hash(i*97));
                dot.a=.12f+.5f*Hash(i*67);
                p.Ellipse(x,y,size+1,size+1,dot,true);
            }
            // Cultural architecture in the distance rather than generic fantasy sky.
            Color stone=Color.Lerp(bottom,style.armor,.24f);
            if(hero != null && hero.origin==Origin.Egypt)
            {
                p.Triangle(15,95,55,95,38,210,stone);
                p.Triangle(190,86,252,86,222,191,stone);
                p.Rect(18,48,65,9,stone);p.Rect(175,45,252,9,stone);
            }
            else if(hero!=null&&hero.origin==Origin.Greece)
            {
                for(int x=18;x<=242;x+=54)
                {
                    p.Rect(x,61,x+18,192,stone);
                    p.Rect(x-3,191,x+21,200,stone);
                    p.Rect(x-6,56,x+24,65,stone);
                }
            }
            else if(hero!=null&&hero.origin==Origin.Norse)
            {
                p.Line(126,43,122,309,8,stone);
                for(int i=0;i<6;i++)p.Line(123,160+i*18,48+i*30,245+i*11,4,stone);
                for(int i=0;i<5;i++)p.Line(129,158+i*22,212-i*20,255+i*9,4,stone);
            }
            else if(hero!=null&&hero.origin==Origin.Japan)
            {
                for(int x=25;x<248;x+=78)
                {
                    p.Rect(x,65,x+7,173,stone);p.Rect(x+43,65,x+50,173,stone);
                    p.Rect(x-9,174,x+59,182,stone);p.Rect(x-4,188,x+53,193,stone);
                }
            }
            else if(hero!=null&&hero.origin==Origin.China)
            {
                for(int i=0;i<7;i++)
                {
                    int x=(int)(i*42+Hash(i)*17);
                    p.Ellipse(x,53+i%3*37,38,14,stone,true);
                }
            }
            else
            {
                for(int x=24;x<256;x+=53)
                {
                    p.Rect(x,20,x+29,120+(x%67),stone);
                    p.Triangle(x-4,120+(x%67),x+33,120+(x%67),
                        x+14,156+(x%67),stone);
                }
            }
            // Elemental halo: circles/rays are material markers not mythical claims.
            if(hero!=null && (hero.rarity==Rarity.UR))
            {
                for(int i=0;i<32;i++)
                {
                    float theta=(float)(i*Math.PI*2/32);
                    float inner=85f, outer=i%4==0?118f:101f;
                    int x1=128+(int)(Mathf.Cos(theta)*inner);
                    int y1=267+(int)(Mathf.Sin(theta)*inner);
                    int x2=128+(int)(Mathf.Cos(theta)*outer);
                    int y2=267+(int)(Mathf.Sin(theta)*outer);
                    Color ray=style.highlight;ray.a=i%4==0?.5f:.22f;
                    p.Line(x1,y1,x2,y2,2,ray,true);
                }
                p.Ring(128,267,77,3,style.highlight,.56f);
            }
        }

        private static void DrawFigure(Painter p, PortraitIdentity a, HeroDefinition hero)
        {
            if(a.species==PortraitSpecies.Serpent){DrawSerpent(p,a);return;}
            if(a.species==PortraitSpecies.Wolf){DrawWolf(p,a);return;}
            if(a.species==PortraitSpecies.Cosmic){DrawCosmos(p,a);return;}
            Color outline=Color.Lerp(a.hair,Color.black,.36f);
            Color gold=Color.Lerp(a.armor,a.highlight,.38f);
            Color skinShadow=Color.Lerp(a.skin,a.hair,.23f);
            // Long cape, hair and upper body form a distinct silhouette.
            p.Triangle(49,28,211,28,141,225,a.fabric);
            p.Triangle(14,28,89,28,74,196,Color.Lerp(a.fabric,a.hair,.28f));
            p.Triangle(165,28,249,28,190,198,Color.Lerp(a.fabric,a.hair,.16f));
            if(a.species==PortraitSpecies.Falcon||a.heroId=="ra"||a.heroId=="isis")
            {
                for(int i=0;i<7;i++)
                {
                    int off=i*8;
                    p.Triangle(38-off,78+off,101,180,35-off,188+off,
                        Color.Lerp(a.highlight,a.fabric,.26f));
                    p.Triangle(214+off,78+off,160,180,215+off,188+off,
                        Color.Lerp(a.highlight,a.fabric,.26f));
                }
            }
            p.Ellipse(128,100,95,99,outline);
            p.Ellipse(128,119,85,87,a.armor);
            p.Triangle(55,66,201,66,130,197,a.armor);
            p.Triangle(88,64,170,64,130,170,a.fabric);
            p.Line(73,141,181,141,6,gold,true);
            p.Line(90,76,163,120,4,gold,true);
            p.Line(163,76,97,120,4,gold,true);
            p.Ellipse(128,177,21,30,a.skin);
            // Hair behind the head, stylized head / crown.
            p.Ellipse(128,266,a.feminine?68:64,a.feminine?89:74,a.hair);
            if(a.feminine)
            {
                p.Ellipse(74,195,23,91,a.hair);
                p.Ellipse(180,195,25,92,a.hair);
            }
            else if(a.heroId=="odin"||a.heroId=="zeus"||a.heroId=="merlin")
            {
                p.Triangle(83,221,170,207,122,163,a.hair);
                p.Triangle(96,210,165,209,137,152,Color.Lerp(a.hair,Color.white,.18f));
            }
            p.Ellipse(128,261,51,65,a.species==PortraitSpecies.Jackal?
                new Color(.12f,.13f,.20f):a.species==PortraitSpecies.Cat?
                    new Color(.17f,.17f,.25f):a.species==PortraitSpecies.Crocodile?
                        new Color(.25f,.43f,.32f):a.species==PortraitSpecies.Lion?
                            new Color(.68f,.44f,.23f):a.species==PortraitSpecies.Falcon?
                                new Color(.86f,.81f,.65f):a.skin);
            p.Ellipse(113,264,4,4,Color.Lerp(outline,a.highlight,.25f));
            p.Ellipse(147,264,4,4,Color.Lerp(outline,a.highlight,.25f));
            if(a.heroId=="odin")p.Ellipse(111,264,8,10,outline);
            p.Triangle(130,261,121,243,139,243,skinShadow);
            p.Line(119,234,142,234,2,skinShadow);
            if(a.species==PortraitSpecies.Jackal || a.species==PortraitSpecies.Cat ||
                a.species==PortraitSpecies.Lion || a.species==PortraitSpecies.Falcon)
                DrawAnimalHead(p,a);
            else if(a.species==PortraitSpecies.Crocodile)
                DrawCrocodileHead(p,a);
            if(a.heroId=="sunwukong"){p.Ellipse(127,249,35,34,a.hair);
                p.Ellipse(128,256,25,28,a.skin);}
            DrawCrown(p,a,gold);
            DrawProp(p,a,outline,gold);
            // Belt jewel or element emblem.
            p.Ellipse(128,129,11,13,a.highlight);
            p.Ellipse(128,129,5,8,Color.Lerp(a.highlight,Color.white,.40f));
        }

        private static void DrawCrown(Painter p,PortraitIdentity a, Color trim)
        {
            switch(a.crown)
            {
                case PortraitCrown.Sun:
                case PortraitCrown.Halo:
                    p.Ring(128,315,a.crown==PortraitCrown.Sun?32:48,4,a.highlight,.90f);
                    if(a.crown==PortraitCrown.Sun)
                        for(int i=0;i<12;i++)
                        {
                            float angle=i*Mathf.PI/6;
                            p.Line(128+(int)(Mathf.Cos(angle)*39),315+(int)(Mathf.Sin(angle)*39),
                                128+(int)(Mathf.Cos(angle)*51),315+(int)(Mathf.Sin(angle)*51),
                                3,a.highlight,true);
                        }
                    break;
                case PortraitCrown.Pharaoh:
                    p.Triangle(74,304,182,304,124,363,trim);
                    p.Line(75,306,182,306,7,a.fabric,true);
                    p.Line(128,302,128,343,3,a.highlight,true);
                    break;
                case PortraitCrown.Horn:
                    p.Triangle(76,304,93,296,54,371,trim);
                    p.Triangle(169,296,185,305,198,370,trim);
                    break;
                case PortraitCrown.Royal:
                    p.Rect(82,304,175,318,trim);
                    p.Triangle(82,318,101,318,93,349,trim);
                    p.Triangle(119,316,138,316,128,363,trim);
                    p.Triangle(153,317,175,317,168,351,trim);
                    break;
                case PortraitCrown.Helm:
                    p.Ellipse(128,310,58,36,a.armor);
                    p.Triangle(121,310,140,310,132,361,a.highlight);
                    break;
                case PortraitCrown.Rune:
                    p.Ring(128,308,45,3,a.highlight,.7f);
                    p.Line(128,317,128,351,4,a.highlight);
                    p.Line(113,335,144,335,3,a.highlight);
                    break;
                case PortraitCrown.Crescent:
                    p.Ring(128,322,38,5,a.highlight,.9f);
                    p.Ellipse(143,336,36,35,a.hair);
                    break;
                case PortraitCrown.Laurel:
                    p.Ring(128,304,46,3,a.highlight,.85f);
                    for(int i=0;i<9;i++)
                        p.Ellipse(82+i*12,308+(i%3)*7,6,12,trim);
                    break;
                case PortraitCrown.Lotus:
                    p.Triangle(95,310,159,310,127,352,a.highlight);
                    p.Triangle(94,308,111,308,73,344,a.armor);
                    p.Triangle(151,308,164,310,183,343,a.armor);
                    break;
            }
        }

        private static void DrawProp(Painter p, PortraitIdentity a, Color outline, Color gold)
        {
            int x=205;Color trim=a.highlight;
            if(a.prop==PortraitProp.None) return;
            if(a.prop==PortraitProp.Orb||a.prop==PortraitProp.Mirror)
            {
                p.Glow(204,222,45,trim,.37f);
                p.Ring(204,222,a.prop==PortraitProp.Orb?21:28,5,trim,.9f);
                p.Ellipse(204,222,8,8,Color.Lerp(trim,Color.white,.5f),true);
                return;
            }
            if(a.prop==PortraitProp.Fan)
            {
                for(int i=0;i<9;i++)
                    p.Line(192,144,170+i*8,241+(i%3)*5,2,gold,true);
                return;
            }
            if(a.prop==PortraitProp.Claws)
            {
                for(int i=0;i<3;i++)
                    p.Triangle(x-12+i*14,130,x+2+i*14,143,x-14+i*14,193,trim);
                return;
            }
            if(a.prop==PortraitProp.Book)
            {
                p.Rect(176,167,236,204,a.fabric);
                p.Line(204,167,204,204,3,gold);
                return;
            }
            p.Line(x,36,x+13,317,8,outline);
            p.Line(x,36,x+13,317,4,gold);
            switch(a.prop)
            {
                case PortraitProp.Sword:
                    p.Triangle(x-1,258,x+23,258,x+12,369,trim);
                    p.Line(x-18,264,x+41,264,6,gold);
                    break;
                case PortraitProp.Spear:
                case PortraitProp.Trident:
                    p.Triangle(x+4,299,x+23,299,x+15,373,trim);
                    if(a.prop==PortraitProp.Trident)
                    {
                        p.Triangle(x-15,301,x,302,x-8,362,gold);
                        p.Triangle(x+28,300,x+42,302,x+34,363,gold);
                    }
                    break;
                case PortraitProp.Hammer:
                    p.Rect(x-29,285,x+48,324,a.armor);
                    p.Line(x-27,286,x+48,286,4,trim);
                    p.Line(x-27,323,x+48,323,4,trim);
                    break;
                case PortraitProp.Bow:
                    p.Line(x-22,153,x+19,322,5,gold,true);
                    p.Line(x-22,153,x-33,219,2,trim);
                    p.Line(x-33,219,x+19,322,2,trim);
                    break;
                case PortraitProp.Staff:
                case PortraitProp.Scepter:
                case PortraitProp.Scythe:
                    p.Glow(x+13,331,24,trim,.24f);
                    p.Ellipse(x+13,330,19,19,trim);
                    p.Ellipse(x+13,330,11,11,a.armor);
                    if(a.prop==PortraitProp.Scythe)
                        p.Triangle(x-25,361,x+12,350,x+34,305,trim);
                    break;
                case PortraitProp.Banner:
                    p.Triangle(x+15,295,x+69,320,x+15,342,a.fabric);
                    p.Line(x+16,341,x+69,320,2,trim);
                    break;
            }
        }

        private static void DrawAnimalHead(Painter p,PortraitIdentity a)
        {
            if(a.species==PortraitSpecies.Cat||a.species==PortraitSpecies.Jackal)
            {
                p.Triangle(84,289,110,288,84,349,a.hair);
                p.Triangle(145,288,171,290,170,349,a.hair);
                p.Triangle(89,302,104,298,91,336,a.highlight);
                p.Triangle(151,298,164,302,166,337,a.highlight);
                p.Triangle(103,243,153,244,129,217,a.hair);
            }
            else if(a.species==PortraitSpecies.Falcon)
            {
                p.Triangle(116,250,171,255,147,223,a.highlight);
                p.Line(102,280,146,280,3,a.highlight,true);
            }
            else if(a.species==PortraitSpecies.Lion)
            {
                for(int i=0;i<12;i++)
                {
                    float ang=(float)(i*2*Math.PI/12);
                    int cx=128+(int)(Mathf.Cos(ang)*63);
                    int cy=268+(int)(Mathf.Sin(ang)*67);
                    p.Triangle(cx-9,cy-9,cx+10,cy-9,
                        cx+(int)(Mathf.Cos(ang)*22),cy+(int)(Mathf.Sin(ang)*21),a.hair);
                }
            }
        }
        private static void DrawCrocodileHead(Painter p,PortraitIdentity a)
        {
            p.Ellipse(158,237,63,21,a.skin);
            p.Triangle(189,234,244,239,188,221,a.armor);
            p.Line(151,226,211,222,2,a.highlight,true);
        }
        private static void DrawSerpent(Painter p, PortraitIdentity a)
        {
            Color dark=Color.Lerp(a.skin,Color.black,.3f);
            for(int i=0;i<7;i++)
            {
                float t=i/6f;
                p.Ring(128+(int)(Mathf.Sin(i*.7f)*27),81+i*18,
                    67-i*5,14,Color.Lerp(a.skin,a.highlight,t*.44f),.95f);
            }
            p.Ellipse(128,256,73,80,a.skin);
            p.Triangle(75,241,127,179,178,242,dark);
            p.Triangle(64,281,111,268,87,363,a.skin);
            p.Triangle(144,270,188,281,169,366,a.skin);
            p.Ellipse(106,272,11,9,a.highlight);
            p.Ellipse(152,272,11,9,a.highlight);
            p.Triangle(108,228,139,204,152,238,a.highlight);
            p.Ring(128,326,62,4,a.highlight,.68f);
        }
        private static void DrawWolf(Painter p, PortraitIdentity a)
        {
            p.Ellipse(128,113,100,95,a.hair);
            p.Ellipse(128,232,82,108,a.skin);
            p.Triangle(45,270,97,257,53,372,a.skin);
            p.Triangle(164,253,212,268,199,372,a.skin);
            p.Triangle(85,205,173,213,130,144,a.hair);
            p.Ellipse(106,265,12,11,a.highlight);
            p.Ellipse(153,265,12,11,a.highlight);
            p.Triangle(108,195,152,194,127,172,a.armor);
            for(int i=0;i<12;i++)
                p.Triangle(54+i*12,74,58+i*12,100,67+i*12,57,a.armor);
            p.Ring(127,280,89,5,a.highlight,.44f);
        }
        private static void DrawCosmos(Painter p, PortraitIdentity a)
        {
            p.Glow(129,224,125,a.highlight,.45f);
            for(int i=0;i<11;i++)
            {
                float ang=i*2*Mathf.PI/11;
                int x=128+(int)(Mathf.Cos(ang)*77);
                int y=207+(int)(Mathf.Sin(ang)*99);
                p.Triangle(x-18,y-18,x+26,y-10,x,y+36,
                    Color.Lerp(a.armor,a.highlight,Hash(i*97)*.5f));
            }
            p.Ellipse(128,233,77,107,a.hair);
            p.Ring(128,244,45,4,a.highlight,.7f);
            p.Ellipse(113,252,8,11,a.highlight);
            p.Ellipse(152,252,8,11,a.highlight);
            p.Ellipse(128,100,85,84,a.fabric);
            p.Ring(128,209,106,6,a.highlight,.6f);
        }

        private sealed class Painter
        {
            private readonly int w, h;
            private readonly Color32[] pixels;
            public Painter(int width,int height)
            {
                w=width;h=height;pixels=new Color32[w*h];
            }
            public void Put(int x,int y,Color color,bool blend=false)
            {
                if(x<0||y<0||x>=w||y>=h)return;
                int i=y*w+x;
                if(blend&&color.a<.999f)
                {
                    Color prior=pixels[i];
                    color=Color.Lerp(prior,color,color.a);
                }
                pixels[i]=color;
            }
            public void Rect(int x0,int y0,int x1,int y1,Color c)
            {
                int left=Mathf.Max(0,Mathf.Min(x0,x1));
                int right=Mathf.Min(w,Mathf.Max(x0,x1));
                int bottom=Mathf.Max(0,Mathf.Min(y0,y1));
                int top=Mathf.Min(h,Mathf.Max(y0,y1));
                for(int y=bottom;y<top;y++)
                    for(int x=left;x<right;x++)Put(x,y,c,true);
            }
            public void Ellipse(int cx,int cy,int rx,int ry,Color c,bool blend=false)
            {
                if(rx<1||ry<1)return;
                for(int y=Mathf.Max(0,cy-ry);y<Mathf.Min(h,cy+ry+1);y++)
                for(int x=Mathf.Max(0,cx-rx);x<Mathf.Min(w,cx+rx+1);x++)
                {
                    float px=(x-cx)/(float)rx,py=(y-cy)/(float)ry;
                    if(px*px+py*py<=1f)Put(x,y,c,blend);
                }
            }
            public void Ring(int cx,int cy,int rad,int thick,Color color,float alpha)
            {
                Color c=color;c.a=alpha;
                for(int y=Mathf.Max(0,cy-rad-thick);y<Mathf.Min(h,cy+rad+thick+1);y++)
                for(int x=Mathf.Max(0,cx-rad-thick);x<Mathf.Min(w,cx+rad+thick+1);x++)
                {
                    float d=Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
                    if(Mathf.Abs(d-rad)<=thick*.5f)Put(x,y,c,true);
                }
            }
            private static int Edge(int ax,int ay,int bx,int by,int x,int y)
            {return (x-ax)*(by-ay)-(y-ay)*(bx-ax);}
            public void Triangle(int ax,int ay,int bx,int by,int cx,int cy,Color c)
            {
                int minX=Mathf.Max(0,Mathf.Min(ax,Mathf.Min(bx,cx)));
                int maxX=Mathf.Min(w-1,Mathf.Max(ax,Mathf.Max(bx,cx)));
                int minY=Mathf.Max(0,Mathf.Min(ay,Mathf.Min(by,cy)));
                int maxY=Mathf.Min(h-1,Mathf.Max(ay,Mathf.Max(by,cy)));
                int sign=Edge(ax,ay,bx,by,cx,cy);
                if(sign==0)return;
                for(int y=minY;y<=maxY;y++)
                for(int x=minX;x<=maxX;x++)
                {
                    int a=Edge(ax,ay,bx,by,x,y);
                    int b=Edge(bx,by,cx,cy,x,y);
                    int d=Edge(cx,cy,ax,ay,x,y);
                    if(sign>0?a>=0&&b>=0&&d>=0:a<=0&&b<=0&&d<=0)
                        Put(x,y,c,true);
                }
            }
            public void Line(int ax,int ay,int bx,int by,int thick,Color c,bool blend=false)
            {
                int steps=Mathf.Max(Mathf.Abs(bx-ax),Mathf.Abs(by-ay));
                for(int i=0;i<=steps;i++)
                {
                    float t=i/(float)Mathf.Max(1,steps);
                    Ellipse(Mathf.RoundToInt(Mathf.Lerp(ax,bx,t)),
                        Mathf.RoundToInt(Mathf.Lerp(ay,by,t)),
                        Mathf.Max(1,thick/2),Mathf.Max(1,thick/2),c,blend);
                }
            }
            public void Glow(int cx,int cy,int rad,Color c,float strength)
            {
                if(rad<1)return;
                for(int y=Mathf.Max(0,cy-rad);y<Mathf.Min(h,cy+rad);y++)
                for(int x=Mathf.Max(0,cx-rad);x<Mathf.Min(w,cx+rad);x++)
                {
                    float d=Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))/(float)rad;
                    float alpha=strength*Mathf.Pow(Mathf.Clamp01(1f-d),2f);
                    Color pixel=c;pixel.a=alpha;
                    Put(x,y,pixel,true);
                }
            }
            public Texture2D ToTexture(string label)
            {
                var tex=new Texture2D(w,h,TextureFormat.RGBA32,false,false);
                tex.name=label;
                tex.wrapMode=TextureWrapMode.Clamp;
                tex.filterMode=FilterMode.Bilinear;
                tex.SetPixels32(pixels);
                tex.Apply(false,true);
                return tex;
            }
        }
    }
}
