using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    /// <summary>
    /// 2D skeletal battle visual assembled at runtime from a hero's portrait art.
    ///
    /// Battle visual priority in HeroVisualDriver.Spawn:
    ///   1. Authored 3D prefab under Resources/Chronicles/Heroes/{id} (production path).
    ///   2. This 2D rig, built from Resources/Chronicles/Portraits art (interim path).
    ///   3. Capsule placeholder, only when the hero has no portrait art at all.
    ///
    /// The rig plays every HeroAnimation the battle requests (Entrance, Idle, Run,
    /// Attack, Skill, Ultimate, Hit, Death, Victory) with procedural bone motion, so
    /// combat shows the actual characters instead of capsules while the final rigs
    /// are produced. It is a development visual, not approved final animation.
    ///
    /// Slicing: by default the whole portrait is a single "body" part. When a sliced
    /// rig exists it is described by Resources/Chronicles/Rigs/{heroId}.json (schema in
    /// docs/BATTLE_2D_HERO_RIGS.md); parts such as head, arms, weapon and cape then
    /// animate semi-independently (arm swings on attacks, head nods on idle, ...).
    /// </summary>
    public sealed class HeroBattleVisual2D : MonoBehaviour
    {
        [Serializable]
        private sealed class RigPartData
        {
            public string name;
            public string parent;
            public string role;
            public float x, y, w = 1f, h = 1f; // UV rect of the part inside the portrait
            public float pivotX = .5f, pivotY = 0f;
            // Subtracts an atlas panel offset before bone positioning: parts may sample
            // a second copy of the artwork (e.g. right half of a cutout atlas) while
            // sitting at the matching spot of the base panel.
            public float sampleOffsetU;
        }

        [Serializable]
        private sealed class RigData
        {
            public float height = 2.1f;
            public RigPartData[] parts;
        }

        private sealed class Part
        {
            public string role;
            public Transform bone;
            public Quaternion restRotation;
        }

        // Battles place the model root at y = 1 (capsule center). Feet belong at y = 0.
        private const float RootGroundOffset = -1f;

        private readonly List<Part> parts = new List<Part>();
        private readonly Dictionary<string, Transform> bonesByName =
            new Dictionary<string, Transform>(StringComparer.Ordinal);
        private Transform motion, billboard, body, shadow;
        private Material shadowMaterial;
        private float shadowBaseScale = 1.4f;
        private Camera cachedCamera;
        private Material material;
        private Color elementColor = Color.white;
        private HeroAnimation loop = HeroAnimation.Idle;
        private HeroAnimation action = HeroAnimation.Idle;
        private bool hasAction, dead, actionIsLooping, hidden;
        private float actionTime, time, alpha = 1f, idlePhase;
        private static Texture2D shadowTexture;

        public static HeroBattleVisual2D Create(HeroDefinition hero, string skinId)
        {
            if (hero == null) return null;
            // A sliced rig (Rigs/{id}.json) pairs with a processed cutout atlas
            // (BattleCutouts/{id}.png, see docs/BATTLE_2D_HERO_RIGS.md). Without a
            // rig, the plain portrait is the single-part fallback. Original
            // portraits are never modified.
            bool hasRig;
            RigData rig = LoadRig(hero.id, out hasRig);
            Texture2D art = hasRig ?
                Resources.Load<Texture2D>("Chronicles/BattleCutouts/" + hero.id) : null;
            if (art == null) art = ChroniclesPortraitAssets.Static(hero.id, skinId);
            if (art == null) return null;
            GameObject root = new GameObject(hero.name + " - 2D battle rig");
            HeroBattleVisual2D visual = root.AddComponent<HeroBattleVisual2D>();
            visual.Build(hero, art, rig);
            return visual;
        }

        private void Build(HeroDefinition hero, Texture2D art, RigData rig)
        {
            elementColor = HeroSkillEffects.ColorFor(hero.element);
            // Desync idle bobbing between heroes so the line never pumps in unison.
            idlePhase = (hero.id == null ? 0 : hero.id.GetHashCode() & 1023) / 1023f *
                Mathf.PI * 2f;
            Shader shader = ChroniclesShaders.FindAny(
                "Sprites/Default", "Unlit/Transparent", "Standard");
            if (shader != null)
            {
                material = new Material(shader);
                material.mainTexture = art;
            }

            motion = NewNode("Motion", transform, new Vector3(0f, RootGroundOffset, 0f));
            billboard = NewNode("Billboard", motion, Vector3.zero);
            body = NewNode("Body", billboard, Vector3.zero);
            bonesByName.Add("body", body);

            float height = Mathf.Max(.5f, rig.height);
            RigPartData[] partData = rig.parts == null || rig.parts.Length == 0 ?
                new[] { new RigPartData { name = "body", role = "body" } } : rig.parts;

            // The "body" part defines the character frame in UV space: its rect maps
            // to [-width/2..width/2] x [0..height] in world units. Every other part's
            // bone is placed where its pivot sits inside that frame.
            RigPartData bodyData = partData[0];
            foreach (RigPartData candidate in partData)
                if (candidate.role == "body") { bodyData = candidate; break; }
            float bu = bodyData.x, bv = bodyData.y;
            float bw = Mathf.Max(.001f, bodyData.w), bh = Mathf.Max(.001f, bodyData.h);
            // Frame aspect comes from the body rect, not the whole texture: a cutout
            // atlas packs extra panels beside the body artwork, so the full-texture
            // aspect would stretch the character horizontally.
            float width = height * (bw * art.width) / Mathf.Max(1f, bh * art.height);
            Dictionary<string, Vector3> charPositions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);

            foreach (RigPartData data in partData)
            {
                string partName = string.IsNullOrEmpty(data.name) ?
                    "part" + parts.Count : data.name;
                float partW = Mathf.Max(.001f, data.w) / bw * width;
                float partH = Mathf.Max(.001f, data.h) / bh * height;
                float pivotU = data.x + data.pivotX * data.w - data.sampleOffsetU;
                float pivotV = data.y + data.pivotY * data.h;
                Vector3 charPos = new Vector3(
                    ((pivotU - bu) / bw - .5f) * width,
                    ((pivotV - bv) / bh) * height, 0f);
                Vector3 parentPos = Vector3.zero;
                Transform parent = body;
                Transform found;
                if (!string.IsNullOrEmpty(data.parent) &&
                    bonesByName.TryGetValue(data.parent, out found))
                {
                    parent = found;
                    Vector3 pp;
                    if (charPositions.TryGetValue(data.parent, out pp)) parentPos = pp;
                }
                Transform bone = NewNode(partName, parent, charPos - parentPos);
                bonesByName[partName] = bone;
                charPositions[partName] = charPos;

                // Vertices are placed so the part pivot sits on the bone origin.
                float x0 = -data.pivotX * partW;
                float y0 = -data.pivotY * partH;
                Mesh mesh = QuadMesh(x0, y0, x0 + partW, y0 + partH,
                    data.x, data.y, data.x + data.w, data.y + data.h);
                GameObject quad = new GameObject(partName + " mesh");
                quad.transform.SetParent(bone, false);
                MeshFilter filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
                if (material != null) renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                parts.Add(new Part
                {
                    role = data.role ?? "",
                    bone = bone,
                    restRotation = bone.localRotation
                });
            }
            BuildShadow(width);
        }

        // Soft contact shadow: the single biggest depth cue for 2D characters on a
        // 3D floor (AFK Arena/Omniheroes style). Lies flat under the character,
        // follows attack lunges horizontally but stays on the ground during jumps.
        private void BuildShadow(float width)
        {
            if (shadowTexture == null)
            {
                const int size = 64;
                shadowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int py = 0; py < size; py++)
                    for (int px = 0; px < size; px++)
                    {
                        float dx = (px - size / 2f) / (size / 2f);
                        float dy = (py - size / 2f) / (size / 2f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * .55f;
                        shadowTexture.SetPixel(px, py, new Color(0f, 0f, 0f, a));
                    }
                shadowTexture.Apply();
            }
            Shader shader = ChroniclesShaders.FindAny(
                "Sprites/Default", "Unlit/Transparent", "Standard");
            if (shader == null) return;
            shadowMaterial = new Material(shader);
            shadowMaterial.mainTexture = shadowTexture;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "contact shadow";
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = shadowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            shadow = quad.transform;
            shadow.SetParent(motion, false);
            shadow.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadowBaseScale = Mathf.Max(.8f, width * 1.45f);
            shadow.localScale = new Vector3(shadowBaseScale, shadowBaseScale * .8f, 1f);
            shadow.localPosition = new Vector3(0f, .04f, 0f);
        }

        private static RigData LoadRig(string heroId, out bool hasRig)
        {
            TextAsset json = Resources.Load<TextAsset>("Chronicles/Rigs/" + heroId);
            hasRig = json != null;
            if (json == null) return new RigData();
            try
            {
                RigData parsed = JsonUtility.FromJson<RigData>(json.text);
                return parsed ?? new RigData();
            }
            catch (Exception)
            {
                return new RigData();
            }
        }

        private static Transform NewNode(string name, Transform parent, Vector3 localPosition)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent, false);
            node.transform.localPosition = localPosition;
            return node.transform;
        }

        private static Mesh QuadMesh(float x0, float y0, float x1, float y1,
            float u0, float v0, float u1, float v1)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f),
                new Vector3(x0, y1, 0f), new Vector3(x1, y1, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(u0, v0), new Vector2(u1, v0),
                new Vector2(u0, v1), new Vector2(u1, v1)
            };
            mesh.normals = new[]
            {
                new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f)
            };
            // Grounding gradient: feet sit slightly darker/cooler than the head,
            // which reads as ambient occlusion on unlit sprite shaders that
            // multiply vertex color (Sprites/Default does; ignored otherwise).
            mesh.colors = new[]
            {
                new Color(.72f, .72f, .80f), new Color(.72f, .72f, .80f),
                Color.white, Color.white
            };
            // Both windings: the billboard must stay visible whichever way it turns.
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 };
            return mesh;
        }

        public void Play(HeroAnimation animation)
        {
            if (dead) return;
            if (animation == HeroAnimation.Idle || animation == HeroAnimation.Run)
            {
                loop = animation;
                return;
            }
            action = animation;
            actionTime = 0f;
            hasAction = true;
            actionIsLooping = animation == HeroAnimation.Victory ||
                animation == HeroAnimation.Death;
            if (animation == HeroAnimation.Death) dead = true;
        }

        private static float Duration(HeroAnimation animation)
        {
            switch (animation)
            {
                case HeroAnimation.Entrance: return .55f;
                case HeroAnimation.Attack: return .45f;
                case HeroAnimation.Skill: return .8f;
                case HeroAnimation.Ultimate: return 1.15f;
                case HeroAnimation.Hit: return .3f;
                case HeroAnimation.Death: return 1.5f;
                default: return .5f;
            }
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void FaceCamera()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null)
                    cachedCamera = FindFirstObjectByType<Camera>();
                if (cachedCamera == null) return;
            }
            Camera cam = cachedCamera;
            Vector3 away = billboard.position - cam.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < .0001f) return;
            billboard.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        private void Update()
        {
            if (hidden || billboard == null) return;
            float dt = Time.deltaTime;
            time += dt;
            FaceCamera();

            Vector3 motionOffset = Vector3.zero;
            float tilt = 0f, roll = 0f, punch = 0f, flash = 0f;
            float targetAlpha = 1f;
            Color flashColor = Color.white;

            if (!dead)
            {
                if (loop == HeroAnimation.Run)
                {
                    motionOffset.y = Mathf.Abs(Mathf.Sin(time * 11f)) * .09f;
                    tilt = 8f;
                    roll = Mathf.Sin(time * 11f) * 2.5f;
                }
                else
                {
                    punch = Mathf.Sin((time + idlePhase) * 2.3f) * .015f;
                    roll = Mathf.Sin((time + idlePhase) * 1.1f) * 1.1f;
                }
            }

            float actionT = 0f;
            if (hasAction)
            {
                actionTime += dt;
                float duration = Duration(action);
                actionT = actionIsLooping ? actionTime / duration :
                    Mathf.Clamp01(actionTime / duration);
                float t = actionT;
                switch (action)
                {
                    case HeroAnimation.Entrance:
                        targetAlpha = Mathf.Clamp01(t * 1.7f);
                        punch += Mathf.Sin(Mathf.Min(1f, t * 1.15f) * Mathf.PI) * .12f;
                        break;
                    case HeroAnimation.Attack:
                    {
                        // Genre timing: brief anticipation pull-back, overshooting
                        // strike, then recovery. Contact frame squashes the body.
                        float lunge;
                        if (t < .18f) lunge = -.14f * Smooth(t / .18f);
                        else if (t < .48f)
                            lunge = Mathf.Lerp(-.14f, 1.06f, Smooth((t - .18f) / .30f));
                        else lunge = 1.06f * (1f - Smooth((t - .48f) / .52f));
                        motionOffset.z += lunge * .6f;
                        tilt += Mathf.Max(0f, lunge) * 12f + Mathf.Min(0f, lunge) * 20f;
                        float contact = Mathf.Clamp01(1f - Mathf.Abs(t - .48f) / .1f);
                        punch -= contact * .09f;
                        break;
                    }
                    case HeroAnimation.Skill:
                        motionOffset.y += Mathf.Sin(t * Mathf.PI) * .14f;
                        punch += Mathf.Sin(t * Mathf.PI) * .09f;
                        flash = Mathf.Sin(t * Mathf.PI) * .75f;
                        flashColor = new Color(1f, .85f, .35f);
                        break;
                    case HeroAnimation.Ultimate:
                        punch += Mathf.Sin(t * Mathf.PI) * .2f;
                        motionOffset.x += Mathf.Sin(t * 43f) * .05f * (1f - t);
                        flash = Mathf.Sin(t * Mathf.PI);
                        flashColor = elementColor;
                        break;
                    case HeroAnimation.Hit:
                        motionOffset.z -= Mathf.Sin(t * Mathf.PI) * .38f;
                        motionOffset.y += Mathf.Sin(t * Mathf.PI) * .07f;
                        tilt -= Mathf.Sin(t * Mathf.PI) * 9f;
                        flash = Mathf.Sin(t * Mathf.PI);
                        flashColor = new Color(1f, .25f, .2f);
                        break;
                    case HeroAnimation.Death:
                    {
                        float fall = Smooth(Mathf.Min(1f, t * 1.25f));
                        tilt -= fall * 84f;
                        targetAlpha = 1f - Smooth(Mathf.Clamp01((t - .35f) / .65f));
                        break;
                    }
                    case HeroAnimation.Victory:
                        motionOffset.y += Mathf.Abs(Mathf.Sin(actionTime * 6.8f)) * .24f;
                        punch += Mathf.Sin(actionTime * 13.6f) * .04f;
                        break;
                }
                if (!actionIsLooping && actionT >= 1f) hasAction = false;
            }

            alpha = targetAlpha;
            motion.localPosition = new Vector3(0f, RootGroundOffset, 0f) + motionOffset;
            body.localRotation = Quaternion.Euler(tilt, 0f, roll);
            body.localScale = new Vector3(1f - punch * .5f, 1f + punch, 1f);

            bool striking = hasAction && (action == HeroAnimation.Attack ||
                action == HeroAnimation.Skill || action == HeroAnimation.Ultimate);
            float swing = striking ? Mathf.Sin(Mathf.Clamp01(actionT) * Mathf.PI) : 0f;
            foreach (Part part in parts)
            {
                Quaternion extra = Quaternion.identity;
                if (part.role == "armFront" || part.role == "weapon")
                    // Modest swings: sliced overlay parts show their seam when
                    // rotated far (verified on the Ra rig, docs/BATTLE_2D_HERO_RIGS.md).
                    extra = Quaternion.Euler(0f, 0f, (part.role == "weapon" ? -70f : -35f) * swing);
                else if (part.role == "armBack")
                    extra = Quaternion.Euler(0f, 0f, 40f * swing);
                else if (part.role == "head")
                    extra = Quaternion.Euler(Mathf.Sin((time + idlePhase) * 2.1f) * 3f, 0f, 0f);
                else if (part.role == "cape")
                    extra = Quaternion.Euler(Mathf.Sin((time + idlePhase) * 3.1f) * 5f, 0f, 0f);
                part.bone.localRotation = part.restRotation * extra;
            }

            if (shadow != null)
            {
                shadow.localPosition = new Vector3(0f, .04f - motionOffset.y, 0f);
                float ground = Mathf.Clamp01(1f - motionOffset.y * 1.6f);
                float s = shadowBaseScale * Mathf.Lerp(.82f, 1f, ground);
                shadow.localScale = new Vector3(s, s * .8f, 1f);
                if (shadowMaterial != null)
                {
                    Color sc = new Color(1f, 1f, 1f, alpha * Mathf.Lerp(.45f, 1f, ground));
                    shadowMaterial.color = sc;
                }
            }

            Color tinted = Color.Lerp(Color.white, flashColor, flash);
            tinted.a = alpha;
            if (material != null) material.color = tinted;

            if (dead && alpha <= .01f)
            {
                hidden = true;
                motion.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (shadowMaterial != null) Destroy(shadowMaterial);
        }
    }
}
