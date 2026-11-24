using System;
using UnityEngine;

namespace MythicChronicles
{
    public enum HeroAnimation { Entrance, Idle, Run, Attack, Skill, Ultimate, Hit, Death, Victory }

    /// <summary>
    /// Production art integration contract.
    /// Prefabs can be placed at Resources/Chronicles/Heroes/{id}/{skin}.prefab
    /// or Resources/Chronicles/Heroes/{id}.prefab (default fallback).
    /// Animator triggers should be: Entrance, Idle, Run, Attack, Skill, Ultimate, Hit, Death, Victory.
    /// Optional VFX live at Resources/Chronicles/VFX/Entrances/{id}.
    /// When no prefab exists, a 2D rig is assembled from the hero's portrait art
    /// (HeroBattleVisual2D); the capsule remains the last-resort placeholder only
    /// for heroes with no portrait art at all.
    /// </summary>
    public sealed class HeroVisualDriver : MonoBehaviour
    {
        private Animator animator;
        private HeroBattleVisual2D rig2d;
        private HeroAnimation currentLoop;
        private bool loopInitialized;
        public bool IsPlaceholder { get; private set; }

        public static HeroVisualDriver Spawn(HeroDefinition hero, string skin, Transform parent,
            Material fallbackMaterial)
        {
            if (hero == null) throw new ArgumentNullException("hero");
            string safeSkin = string.IsNullOrEmpty(skin) ? "default" : skin.Trim();
            // Identifiers are from the static catalog: never accept a path separator from untrusted data.
            if (safeSkin.Contains("/") || safeSkin.Contains("\\") || safeSkin.Contains(".."))
                safeSkin = "default";

            string basePath = "Chronicles/Heroes/" + hero.id;
            GameObject prefab = Resources.Load<GameObject>(basePath + "/" + safeSkin);
            if (prefab == null) prefab = Resources.Load<GameObject>(basePath);
            GameObject view = null;
            bool placeholder = false;
            if (prefab != null)
            {
                view = Instantiate(prefab);
                view.name = hero.name + " - " + safeSkin;
            }
            if (view == null)
            {
                // Interim path: a 2D skeletal rig from the portrait art, so roster
                // heroes fight as their characters while the 3D rigs are produced.
                HeroBattleVisual2D rig = HeroBattleVisual2D.Create(hero, safeSkin);
                if (rig != null) view = rig.gameObject;
            }
            if (view == null)
            {
                placeholder = true;
                view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                view.name = hero.name + " - ART PLACEHOLDER";
                Renderer renderer = view.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Unlit, dark placeholder: the lit fallback under the arena
                    // light blew out to a white ghost on device (Khalid's video).
                    Shader flat = ChroniclesShaders.FindAny(
                        "Sprites/Default", "Unlit/Transparent");
                    if (flat != null)
                    {
                        Material ph = new Material(flat);
                        ph.color = new Color(.38f, .20f, .24f, 1f);
                        renderer.sharedMaterial = ph;
                    }
                    else if (fallbackMaterial != null) renderer.sharedMaterial = fallbackMaterial;
                }
                Collider collider = view.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            view.transform.SetParent(parent, false);
            HeroVisualDriver driver = view.GetComponent<HeroVisualDriver>();
            if (driver == null) driver = view.AddComponent<HeroVisualDriver>();
            driver.IsPlaceholder = placeholder;
            driver.animator = view.GetComponentInChildren<Animator>(true);
            driver.rig2d = view.GetComponent<HeroBattleVisual2D>();
            driver.currentLoop = HeroAnimation.Idle;
            driver.loopInitialized = false;
            return driver;
        }

        public void EntranceFX(string id)
        {
            GameObject prefab = Resources.Load<GameObject>("Chronicles/VFX/Entrances/" + id);
            if (prefab == null) return;
            GameObject effect = Instantiate(prefab, transform, false);
            effect.transform.localPosition = Vector3.zero;
            Destroy(effect, 7f);
        }

        public void Play(HeroAnimation animation)
        {
            if (rig2d != null)
            {
                rig2d.Play(animation);
                return;
            }
            if (animator == null) return;
            bool looping = animation == HeroAnimation.Idle || animation == HeroAnimation.Run;
            if (looping && loopInitialized && currentLoop == animation) return;
            if (looping) { currentLoop = animation; loopInitialized = true; }

            string key = animation.ToString();
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == key && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger(key);
                    return;
                }
            }
            // If a supplied animation controller has no matching trigger, no exception is raised.
        }
    }
}
