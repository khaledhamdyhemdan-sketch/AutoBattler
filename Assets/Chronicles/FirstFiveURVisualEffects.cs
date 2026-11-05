using System;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// First-five UR skill/ultimate VFX adapter. Production VFX prefabs override
    /// this minimalist procedural preview at:
    /// Resources/Chronicles/VFX/Skills/{heroId}
    /// Resources/Chronicles/VFX/Ultimates/{heroId}
    /// </summary>
    public sealed class FirstFiveURVisualEffects : MonoBehaviour
    {
        private string heroId;
        private bool ultimate;
        private Transform[] pieces;
        private Vector3[] baseScales;
        private Material material;
        private Vector3 source, destination;
        private float elapsed, duration;
        private Color visualColor;

        public static void Play(string heroId, bool ultimate, Transform caster, Transform target)
        {
            if (!IsSupported(heroId) || caster == null) return;
            string folder = ultimate ? "Ultimates" : "Skills";
            GameObject authored = Resources.Load<GameObject>(
                "Chronicles/VFX/" + folder + "/" + heroId);
            if (authored != null)
            {
                Vector3 position = target == null ? caster.position : target.position;
                GameObject live = Instantiate(authored, position, Quaternion.identity);
                live.name = heroId + (ultimate ? " Ultimate (authored)" : " Skill (authored)");
                Destroy(live, 7f);
                return;
            }
            // The fallback is a development VFX cue, NOT finished animation.
            GameObject root = new GameObject(heroId + (ultimate ? " UR ultimate preview" : " UR skill preview"));
            FirstFiveURVisualEffects effect = root.AddComponent<FirstFiveURVisualEffects>();
            effect.Initialize(heroId, ultimate, caster.position,
                target == null ? caster.position : target.position);
        }

        private static bool IsSupported(string id)
        {
            return id == "ra" || id == "apep" || id == "odin" ||
                id == "hades" || id == "amaterasu";
        }

        private void Initialize(string id, bool isUltimate, Vector3 start, Vector3 end)
        {
            heroId = id; ultimate = isUltimate; source = start; destination = end;
            duration = ultimate ? 2.8f : 1.35f;
            visualColor = id == "ra" ? new Color(1f, .74f, .18f, .8f) :
                id == "apep" ? new Color(.47f, .20f, .78f, .8f) :
                id == "odin" ? new Color(.38f, .81f, .98f, .8f) :
                id == "hades" ? new Color(.53f, .29f, .69f, .8f) :
                new Color(1f, .75f, .66f, .8f);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) { Destroy(gameObject); return; }
            material = new Material(shader);
            material.color = visualColor;
            int count = ultimate ? 12 : 6;
            pieces = new Transform[count];
            baseScales = new Vector3[count];
            transform.position = destination;
            for (int i = 0; i < count; i++)
            {
                PrimitiveType shape =
                    id == "odin" || id == "hades" ? PrimitiveType.Cube :
                    id == "apep" ? PrimitiveType.Capsule :
                    id == "amaterasu" ? PrimitiveType.Quad :
                    PrimitiveType.Sphere;
                GameObject part = GameObject.CreatePrimitive(shape);
                part.name = id + " UR FX shard " + i;
                part.transform.SetParent(transform, false);
                Renderer renderer = part.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
                Collider collider = part.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                pieces[i] = part.transform;
                if (id == "odin")
                    baseScales[i] = new Vector3(.14f, .48f, .08f);
                else if (id == "hades")
                    baseScales[i] = new Vector3(.11f, .74f, .12f);
                else if (id == "apep")
                    baseScales[i] = new Vector3(.18f, .38f, .18f);
                else if (id == "amaterasu")
                    baseScales[i] = new Vector3(.43f, .28f, 1f);
                else
                    baseScales[i] = Vector3.one * .22f;
            }
        }

        private void Update()
        {
            if (pieces == null) return;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, t * 6f));
            float disappear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) * 4f));
            float size = Mathf.Max(.001f, reveal * disappear);
            int count = pieces.Length;
            for (int i = 0; i < count; i++)
            {
                if (pieces[i] == null) continue;
                float a = i * Mathf.PI * 2f / count + t * (ultimate ? 5f : 3f);
                float r = Mathf.Lerp(.25f, ultimate ? 3.0f : 1.55f, t);
                Vector3 offset;
                if (heroId == "apep")
                    offset = new Vector3(Mathf.Sin(a * 1.3f) * r, .7f +
                        Mathf.Sin(a * 2f + t * 9f) * .5f, Mathf.Cos(a) * r);
                else if (heroId == "odin")
                    offset = new Vector3(Mathf.Cos(a) * r, 1.15f +
                        Mathf.Sin(a * 2f) * .18f, Mathf.Sin(a) * r);
                else if (heroId == "hades")
                    offset = new Vector3(Mathf.Cos(a) * r * .7f,
                        .35f + (i % 3) * .55f, Mathf.Sin(a) * r * .65f);
                else if (heroId == "amaterasu")
                    offset = new Vector3(Mathf.Cos(a) * r,
                        .8f + Mathf.Sin(a * 1.6f + t * 3f) * .55f,
                        Mathf.Sin(a) * r);
                else
                    offset = new Vector3(Mathf.Cos(a) * r,
                        1f + Mathf.Sin(a + t * 10f) * .35f,
                        Mathf.Sin(a) * r);
                pieces[i].localPosition = offset;
                pieces[i].localRotation = Quaternion.Euler(
                    t * 220f + i * 17f, t * 300f + i * 26f, t * 140f);
                pieces[i].localScale = baseScales[i] * (size * (ultimate ? 1.3f : 1f));
            }
            if (t >= 1f) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }
    }
}
