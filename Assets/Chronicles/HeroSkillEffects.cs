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
        private Material material;
        private Color effectColor;
        private float elapsed, duration;
        private bool ultimate;

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
            effect.Initialize(ColorFor(hero.element), isUltimate);
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

        private void Initialize(Color color, bool isUltimate)
        {
            effectColor = color;
            ultimate = isUltimate;
            duration = isUltimate ? 1.6f : .9f;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Destroy(gameObject); return; }
            material = new Material(shader);
            int count = isUltimate ? 10 : 6;
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
                baseScales[i] = new Vector3(.3f, .3f, 1f);
            }
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
            int count = pieces.Length;
            for (int i = 0; i < count; i++)
            {
                if (pieces[i] == null) continue;
                float angle = i * Mathf.PI * 2f / count + t * 2.4f;
                float radius = Mathf.Lerp(.2f, ultimate ? 2.6f : 1.4f, t);
                pieces[i].localPosition = new Vector3(Mathf.Cos(angle) * radius,
                    .9f + Mathf.Sin(t * Mathf.PI) * (ultimate ? .8f : .35f),
                    Mathf.Sin(angle) * radius);
                pieces[i].localRotation = Quaternion.Euler(
                    t * 160f + i * 23f, t * 240f + i * 31f, 0f);
                pieces[i].localScale = baseScales[i] *
                    Mathf.Max(.001f, grow * fade) * (ultimate ? 1.5f : 1f);
            }
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
