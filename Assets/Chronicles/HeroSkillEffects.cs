using System;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// Skill/ultimate cast effects for every hero. Priority:
    ///   1. Authored VFX prefab at Resources/Chronicles/VFX/{Skills|Ultimates}/{heroId}.
    ///   2. The hand-tuned first-five UR preview (FirstFiveURVisualEffects).
    ///   3. A generic element-colored burst generated here, so every roster hero has a
    ///      visible cast while production VFX are authored. Development cue, not final VFX.
    /// </summary>
    public sealed class HeroSkillEffects : MonoBehaviour
    {
        private Transform[] pieces;
        private Vector3[] baseScales;
        private Transform ring, beamA, beamB;
        private Material material;
        private Color effectColor;
        private float elapsed, duration;
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
            if (FirstFiveURVisualEffects.IsSupported(hero.id))
            {
                FirstFiveURVisualEffects.Play(hero.id, isUltimate, caster, target);
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
                case Rarity.EX: return 4;
                default: return 1;
            }
        }

        private void Initialize(Color color, bool isUltimate, int rarityTier = 1)
        {
            effectColor = color;
            ultimate = isUltimate;
            tier = Mathf.Clamp(rarityTier, 1, 4);
            duration = (isUltimate ? 1.6f : .9f) * (1f + (tier - 1) * .12f);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Destroy(gameObject); return; }
            material = new Material(shader);
            int count = (isUltimate ? 10 : 6) + (tier - 1) * 3;
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
            if (tier >= 2) ring = MakePiece("ground ring");
            if (tier >= 3)
            {
                beamA = MakePiece("beam A");
                beamB = MakePiece("beam B");
            }
        }

        private Transform MakePiece(string name)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Quad);
            piece.name = "skill FX " + name;
            piece.transform.SetParent(transform, false);
            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
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
                pieces[i].localRotation = Quaternion.Euler(
                    t * 160f + i * 23f, t * 240f + i * 31f, 0f);
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

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
