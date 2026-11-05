#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MythicChronicles;

namespace MythicChronicles.Editor
{
    public static class ChroniclesProjectSetup
    {
        private const string ScenePath = "Assets/Chronicles/Chronicles_Main.unity";

        [MenuItem("Tools/Chronicles/Create Main Game Scene")]
        public static void CreateMainGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Chronicles Game Root");
            root.AddComponent<ChroniclesGame>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[Chronicles] Failed to create main scene.");
                return;
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Chronicles] Main portrait game scene created and added as first Build Settings scene.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        [MenuItem("Tools/Chronicles/Validate Game Catalog")]
        public static void ValidateGameCatalog()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var originCounts = new Dictionary<Origin, int>();
            int errors = 0;
            foreach (var hero in WorldsCatalog.Heroes)
            {
                if (string.IsNullOrWhiteSpace(hero.id) || !seen.Add(hero.id))
                {
                    Debug.LogError("[Chronicles] Duplicate or empty hero ID: " + hero.id);
                    errors++;
                }
                if (string.IsNullOrWhiteSpace(hero.story) || string.IsNullOrWhiteSpace(hero.name))
                {
                    Debug.LogError("[Chronicles] Hero needs name and lore: " + hero.id);
                    errors++;
                }
                if (!originCounts.ContainsKey(hero.origin)) originCounts[hero.origin] = 0;
                originCounts[hero.origin]++;
            }
            foreach (var id in WorldsCatalog.StartingHeroes)
            {
                if (WorldsCatalog.FindHero(id) == null)
                {
                    Debug.LogError("[Chronicles] Starter not in catalog: " + id);
                    errors++;
                }
            }
            if (WorldsCatalog.Heroes.Length != 41)
            {
                Debug.LogError("[Chronicles] Expected 41 named heroes."); errors++;
            }
            var kitIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (HeroKit kit in HeroAbilityCatalog.All)
            {
                if (WorldsCatalog.FindHero(kit.heroId) == null || !kitIds.Add(kit.heroId))
                {
                    Debug.LogError("[Chronicles] Unknown/duplicate ability kit: " + kit.heroId);
                    errors++;
                }
            }
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                if (HeroAbilityCatalog.For(hero.id) == null)
                {
                    Debug.LogError("[Chronicles] Hero has no skill kit: " + hero.id);
                    errors++;
                }
            if (HeroPortraitIdentityCatalog.All.Length != WorldsCatalog.Heroes.Length)
            {
                Debug.LogError("[Chronicles] Every hero needs a unique static/motion portrait identity.");
                errors++;
            }
            var portraitIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (PortraitIdentity portrait in HeroPortraitIdentityCatalog.All)
                if (WorldsCatalog.FindHero(portrait.heroId) == null ||
                    !portraitIds.Add(portrait.heroId))
                {
                    Debug.LogError("[Chronicles] Unknown or duplicate portrait: " + portrait.heroId);
                    errors++;
                }
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                if (!ChroniclesHeroPortrait.HasIdentity(hero.id))
                {
                    Debug.LogError("[Chronicles] Missing hero portrait: " + hero.id);
                    errors++;
                }
            int rCount = 0, srCount = 0, ssrCount = 0, urCount = 0, exCount = 0;
            int lightCount = 0, darknessCount = 0;
            var urByOrigin = new Dictionary<Origin, int[]>();
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
            {
                if (hero.element != HeroElements.For(hero.id))
                {
                    Debug.LogError("[Chronicles] Hero element mapping mismatch: " + hero.id);
                    errors++;
                }
                switch (hero.rarity)
                {
                    case Rarity.R: rCount++; break;
                    case Rarity.SR: srCount++; break;
                    case Rarity.SSR: ssrCount++; break;
                    case Rarity.UR: urCount++; break;
                    case Rarity.EX: exCount++; break;
                }
                if (hero.element == HeroElement.Light) lightCount++;
                if (hero.element == HeroElement.Darkness) darknessCount++;
                if (HeroElements.IsSpecial(hero) != (hero.rarity == Rarity.UR))
                {
                    Debug.LogError("[Chronicles] Only UR heroes may have Light/Dark: " + hero.id);
                    errors++;
                }
                URCardIdentity card = URCardCatalog.For(hero.id);
                if ((hero.rarity == Rarity.UR) != (card != null))
                {
                    Debug.LogError("[Chronicles] UR card identity missing or set on non-UR: " + hero.id);
                    errors++;
                }
                if (card != null && card.element != hero.element)
                {
                    Debug.LogError("[Chronicles] UR card must match Light/Dark element: " + hero.id);
                    errors++;
                }
                if (hero.rarity == Rarity.UR)
                {
                    int[] counts;
                    if (!urByOrigin.TryGetValue(hero.origin, out counts))
                    {
                        counts = new int[2];
                        urByOrigin.Add(hero.origin, counts);
                    }
                    if (hero.element == HeroElement.Light) counts[0]++;
                    else if (hero.element == HeroElement.Darkness) counts[1]++;
                }
            }
            if (rCount != 2 || srCount != 7 || ssrCount != 16 ||
                urCount != 16 || exCount != 0 || lightCount != 9 || darknessCount != 7 ||
                urByOrigin.Count != 5 || URCardCatalog.Count != 16)
            {
                Debug.LogError("[Chronicles] R/SR/SSR/UR/EX roster allocation mismatch.");
                errors++;
            }
            foreach (var pair in urByOrigin)
                if (pair.Value[0] < 1 || pair.Value[1] < 1)
                {
                    Debug.LogError("[Chronicles] UR realm must include at least one Light and one Dark:  " + pair.Key);
                    errors++;
                }
            if (ChroniclesEquipment.Templates.Length != 18 ||
                ChroniclesRelics.All.Length != 6)
            {
                Debug.LogError("[Chronicles] Gear and relic catalogs are incomplete.");
                errors++;
            }
            for (int stageId = 1; stageId <= 60; stageId++)
            {
                StoryMoment dialogue = WorldsStory.Get(stageId);
                if (dialogue == null || string.IsNullOrWhiteSpace(dialogue.line))
                {
                    Debug.LogError("[Chronicles] Missing stage narrative: " + stageId);
                    errors++;
                }
            }
            for (int i = 1; i <= 60; i++)
            {
                StageDefinition stage = WorldsCatalog.GetStage(i);
                if (stage == null || stage.chapter != (i - 1) / 10 + 1 || stage.id != i)
                { Debug.LogError("[Chronicles] Invalid stage " + i); errors++; }
            }
            foreach (Origin origin in Enum.GetValues(typeof(Origin)))
            {
                if (!originCounts.ContainsKey(origin))
                {
                    Debug.LogWarning("[Chronicles] No heroes yet for " + origin);
                }
            }
            if (errors == 0)
                Debug.Log("[Chronicles] Catalog validation passed: 41 hero kits, 16 UR heroes across five Light/Dark realms and 60 story stages. This is NOT a Unity/Android playtest.");
            else Debug.LogError("[Chronicles] Catalog validation failed with " + errors + " issues.");
        }
    }
}
#endif
