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
    /// </summary>
    public sealed class HeroVisualDriver : MonoBehaviour
    {
        private Animator animator;
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
            GameObject view;
            bool placeholder = prefab == null;
            if (placeholder)
            {
                view = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                view.name = hero.name + " - ART PLACEHOLDER";
                Renderer renderer = view.GetComponent<Renderer>();
                if (renderer != null && fallbackMaterial != null) renderer.sharedMaterial = fallbackMaterial;
                Collider collider = view.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            else
            {
                view = Instantiate(prefab);
                view.name = hero.name + " - " + safeSkin;
            }
            view.transform.SetParent(parent, false);
            HeroVisualDriver driver = view.GetComponent<HeroVisualDriver>();
            if (driver == null) driver = view.AddComponent<HeroVisualDriver>();
            driver.IsPlaceholder = placeholder;
            driver.animator = view.GetComponentInChildren<Animator>(true);
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
