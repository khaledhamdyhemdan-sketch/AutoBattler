using System;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Skill/ultimate cast effects for every hero. Priority:
    ///   1. Authored VFX prefab at Resources/Chronicles/VFX/{Skills|Ultimates}/{heroId}.
    ///   2. A generic element-colored burst generated here, so every roster hero has a
    ///      visible cast while production VFX are authored. Development cue, not final VFX.
    /// The old first-five UR primitive preview (solid spheres/cubes) rendered as flat
    /// white shapes under bloom and was retired; UR heroes use the tier-4 burst.
    /// </summary>
    public sealed class HeroSkillEffects : MonoBehaviour
    {
        private Transform[] pieces;
        private Vector3[] baseScales;
        private Transform ring, beamA, beamB;
        private Material material;
        private Color effectColor;
        private float elapsed, duration;
        private bool panel;
        private bool ultimate;
        // Rarity drives spectacle: R is a tight element pop, SR adds a ground
        // ring, SSR adds a rising beam, UR/EX get the largest, longest show.
        private int tier = 1;

        public static void Play(HeroDefinition hero, bool isUltimate,
            Transform caster, Transform target)
        {
            if (hero == null || caster == null) return;
            string folder = isUltimate ? "Ultimates" : "Skills";
            GameObject authored = Resources.Load<GameObject>(
                "Chronicles/VFX/" + folder + "/" + hero.id);
            Vector3 at = target == null ? caster.position : target.position;
            if (authored != null)
            {
                GameObject live = Instantiate(authored, at, Quaternion.identity);
                live.name = hero.id + (isUltimate ? " Ultimate (authored)" : " Skill (authored)");
                Destroy(live, 7f);
                return;
            }
            GameObject root = new GameObject(hero.id +
                (isUltimate ? " ultimate FX" : " skill FX"));
            root.transform.position = at;
            HeroSkillEffects effect = root.AddComponent<HeroSkillEffects>();
            effect.Initialize(ColorFor(hero.element), isUltimate, TierFor(hero.rarity));
        }

        public static Color ColorFor(HeroElement element)
        {
            switch (element)
            {
                case HeroElement.Fire: return new Color(1f, .45f, .15f);
                case HeroElement.Water: return new Color(.25f, .55f, 1f);
                case HeroElement.Nature: return new Color(.3f, .85f, .35f);
                case HeroElement.Wind: return new Color(.65f, .95f, .8f);
                case HeroElement.Light: return new Color(1f, .85f, .4f);
                case HeroElement.Darkness: return new Color(.6f, .3f, .85f);
                default: return Color.white;
            }
        }

        private static int TierFor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SR: return 2;
                case Rarity.SSR: return 3;
                case Rarity.UR:
                case Rarity.EX:
                case Rarity.SSRP: return 4;
                default: return 1;
            }
        }


        // Set by the battle when the camera looks flat down the Z axis (panel
        // card wall). Effects then lay out in the XY plane and scale to card size.
        public static bool PanelMode;
        private Material ringMaterial;
        private static Texture2D ringTex;

        private static Texture2D RingTexture()
        {
            if (ringTex != null) return ringTex;
            const int size = 128;
            ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            ringTex.wrapMode = TextureWrapMode.Clamp;
            ringTex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size - .5f, dy = (y + .5f) / size - .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    // thin bright band near the rim, soft inner haze
                    float band = Mathf.Exp(-Mathf.Pow((d - .86f) / .07f, 2f));
                    float haze = Mathf.Clamp01(1f - d) * .12f;
                    float a = Mathf.Clamp01(band * .95f + haze) * (d < 1f ? 1f : 0f);
                    ringTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            ringTex.Apply();
            return ringTex;
        }

        private static Texture2D slashTex;
        // Tapered streak (bright center, fading both ends) for slash arcs.
        private static Texture2D SlashTexture()
        {
            if (slashTex != null) return slashTex;
            const int w = 128, h = 32;
            slashTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            slashTex.wrapMode = TextureWrapMode.Clamp;
            slashTex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + .5f) / w * 2f - 1f, v = (y + .5f) / h * 2f - 1f;
                    float taper = 1f - Mathf.Abs(u);
                    float width = Mathf.Lerp(.05f, .55f, Mathf.Pow(taper, .8f));
                    float a = Mathf.Clamp01(1f - Mathf.Abs(v) / width) * Mathf.Pow(taper, .6f);
                    slashTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            slashTex.Apply();
            return slashTex;
        }

        private Material slashMaterial;
        private Transform[] slashes;

        private static Texture2D glowTex;

        // Soft radial glow texture, generated once: white-hot core fading to
        // transparent, so overlapping quads blend into light instead of hard
        // squares. Runtime-generated so no authored art is required.
        private static Texture2D GlowTexture()
        {
            if (glowTex != null) return glowTex;
            const int size = 64;
            glowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            glowTex.wrapMode = TextureWrapMode.Clamp;
            glowTex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size - .5f, dy = (y + .5f) / size - .5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    // Alpha kept low: additive stacking of many shards must not
                    // saturate to a flat white blob over the cards.
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * .5f;
                    glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            glowTex.Apply();
            return glowTex;
        }

        private static Shader BlendShader()
        {
            // Sprites/Default first: it is always present in builds and alpha-
            // blends with material.color, so overlapping glows stay element-
            // colored. The Legacy particle shaders are not all bundled -
            // captures showed Alpha Blended missing (fell through to additive,
            // which saturates to white cores where shards stack).
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        private void Initialize(Color color, bool isUltimate, int rarityTier = 1)
        {
            effectColor = color;
            ultimate = isUltimate;
            tier = Mathf.Clamp(rarityTier, 1, 4);
            duration = (isUltimate ? 1.6f : .9f) * (1f + (tier - 1) * .12f);
            Shader shader = BlendShader();
            if (shader == null) { Destroy(gameObject); return; }
            material = new Material(shader);
            if (material.HasProperty("_MainTex")) material.mainTexture = GlowTexture();
            // Additive shaders tint via TintColor.
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
            int count = (isUltimate ? 8 : 5) + (tier - 1) * 2;
            pieces = new Transform[count];
            baseScales = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Quad);
                shard.name = "skill FX shard " + i;
                shard.transform.SetParent(transform, false);
                Renderer renderer = shard.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                Collider collider = shard.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                pieces[i] = shard.transform;
                baseScales[i] = new Vector3(.3f, .3f, 1f) * (1f + (tier - 1) * .18f);
            }
            if (PanelMode)
            {
                panel = true;
                ringMaterial = new Material(shader);
                if (ringMaterial.HasProperty("_MainTex")) ringMaterial.mainTexture = RingTexture();
                slashMaterial = new Material(shader);
                if (slashMaterial.HasProperty("_MainTex")) slashMaterial.mainTexture = SlashTexture();
                ring = MakePiece("impact ring", ringMaterial);
                int sc = 2 + tier + (isUltimate ? 2 : 0);
                slashes = new Transform[sc];
                for (int i = 0; i < sc; i++) slashes[i] = MakePiece("slash " + i, slashMaterial);
                return;
            }
            if (tier >= 2) ring = MakePiece("ground ring");
            if (tier >= 3)
            {
                beamA = MakePiece("beam A");
                beamB = MakePiece("beam B");
            }
        }

        private Transform MakePiece(string name, Material mat = null)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
            piece.name = "skill FX " + name;
            piece.transform.SetParent(transform, false);
            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat != null ? mat : material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Collider collider = piece.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return piece.transform;
        }

        private void Update()
        {
            if (pieces == null) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float grow = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 5f));
            float fade = Mathf.Clamp01((1f - t) * 2.2f);
            if (material != null)
            {
                Color c = effectColor;
                c.a = fade * .85f;
                material.color = c;
                if (material.HasProperty("_TintColor"))
                {
                    // No hot-core boost: overlapping additive shards already
                    // saturate; keep the element color readable.
                    Color tint = effectColor;
                    tint.a = fade * .5f;
                    material.SetColor("_TintColor", tint);
                }
            }
            if (panel)
            {
                UpdatePanel(t, fade);
                if (t >= 1f) Destroy(gameObject);
                return;
            }
            float reach = 1f + (tier - 1) * .22f;
            int count = pieces.Length;
            for (int i = 0; i < count; i++)
            {
                if (pieces[i] == null) continue;
                float angle = i * Mathf.PI * 2f / count + t * 2.4f;
                float radius = Mathf.Lerp(.2f, (ultimate ? 2.6f : 1.4f) * reach, t);
                pieces[i].localPosition = new Vector3(Mathf.Cos(angle) * radius,
                    .9f + Mathf.Sin(t * Mathf.PI) * (ultimate ? .8f : .35f),
                    Mathf.Sin(angle) * radius);
                // Spin around Z only: the glow texture is radially symmetric,
                // so this reads as shimmering light instead of turning the quad
                // edge-on into invisible slivers.
                pieces[i].localRotation = Quaternion.Euler(
                    0f, 0f, t * 200f + i * 47f);
                pieces[i].localScale = baseScales[i] *
                    Mathf.Max(.001f, grow * fade) * (ultimate ? 1.5f : 1f);
            }
            // SR+: expanding shockwave ring hugging the floor.
            if (ring != null)
            {
                float rr = Mathf.Lerp(.3f, (ultimate ? 3.4f : 2.2f) * reach,
                    Mathf.SmoothStep(0f, 1f, t));
                ring.localPosition = new Vector3(0f, .06f, 0f);
                ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.localScale = new Vector3(rr, rr, 1f);
            }
            // SSR+: crossed rising beams, a quick pillar of element light.
            if (beamA != null)
            {
                float beamT = Mathf.Clamp01(t / .55f);
                float beamIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(beamT * 2.4f));
                float beamOut = Mathf.Clamp01(1f - beamT);
                float h = (ultimate ? 5.2f : 3.6f) * beamIn;
                beamA.localPosition = new Vector3(0f, h * .5f, 0f);
                beamA.localRotation = Quaternion.Euler(0f, 45f, 0f);
                beamA.localScale = new Vector3(.4f * beamIn * beamOut + .02f, h, 1f);
                beamB.localPosition = beamA.localPosition;
                beamB.localRotation = Quaternion.Euler(0f, -45f, 0f);
                beamB.localScale = beamA.localScale;
            }
            if (t >= 1f) Destroy(gameObject);
        }

        // Panel (flat ortho) layout: everything in the XY plane, facing the camera,
        // sized for a ~2.5-unit card. Burst sparks + impact ring + slash arcs.
        private void UpdatePanel(float t, float fade)
        {
            float big = (ultimate ? 1.5f : 1f) * (1f + (tier - 1) * .2f);
            Color c = effectColor;
            // brighten toward white at the very start for an impact flash
            float flash = Mathf.Clamp01(1f - t * 6f);
            Color hot = Color.Lerp(c, Color.white, flash * .7f);
            hot.a = fade;
            if (ringMaterial != null) ringMaterial.color = hot;
            if (slashMaterial != null) slashMaterial.color = hot;
            int count = pieces.Length;
            for (int i = 0; i < count; i++)
            {
                if (pieces[i] == null) continue;
                float ang = i * Mathf.PI * 2f / count + i * .7f;
                float r = Mathf.Lerp(.1f, 1.6f * big, 1f - Mathf.Pow(1f - t, 2f));
                pieces[i].localPosition = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, -1f);
                pieces[i].localRotation = Quaternion.identity;
                pieces[i].localScale = new Vector3(.55f, .55f, 1f) * Mathf.Max(.001f, fade) * big;
            }
            if (ring != null)
            {
                float rr = Mathf.Lerp(.4f, 3.4f * big, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 1.6f)));
                ring.localPosition = new Vector3(0f, 0f, -1.05f);
                ring.localRotation = Quaternion.identity;
                ring.localScale = new Vector3(rr, rr, 1f);
            }
            if (slashes != null)
            {
                float sIn = Mathf.Clamp01(t / .22f);
                for (int i = 0; i < slashes.Length; i++)
                {
                    if (slashes[i] == null) continue;
                    // staggered strikes crossing the target at different angles
                    float delay = i * .06f;
                    float st = Mathf.Clamp01((t - delay) / .3f);
                    float len = Mathf.Sin(st * Mathf.PI * .5f) * 3.2f * big;
                    float ang = (i % 2 == 0 ? 35f : -35f) + (i / 2) * 28f - 20f;
                    slashes[i].localPosition = new Vector3((i - slashes.Length * .5f) * .12f, 0f, -1.1f - i * .01f);
                    slashes[i].localRotation = Quaternion.Euler(0f, 0f, ang);
                    float thick = .55f * big * (1f - st * .6f);
                    slashes[i].localScale = new Vector3(Mathf.Max(.001f, len), Mathf.Max(.001f, thick) * (st > 0f && st < 1f ? 1f : 0f), 1f);
                }
            }
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (ringMaterial != null) Destroy(ringMaterial);
            if (slashMaterial != null) Destroy(slashMaterial);
        }
    }
}
