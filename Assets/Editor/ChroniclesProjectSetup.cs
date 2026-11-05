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
            if (WorldsCatalog.Heroes.Length != 40)
            {
                Debug.LogError("[Chronicles] Expected 40 named heroes."); errors++;
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
            if (rCount != 2 || srCount != 8 || ssrCount != 20 ||
                urCount != 10 || exCount != 0 || lightCount != 5 || darknessCount != 5 ||
                urByOrigin.Count != 5)
            {
                Debug.LogError("[Chronicles] R/SR/SSR/UR/EX roster allocation mismatch.");
                errors++;
            }
            foreach (var pair in urByOrigin)
                if (pair.Value[0] != 1 || pair.Value[1] != 1)
                {
                    Debug.LogError("[Chronicles] UR realm must include one Light and one Dark: " + pair.Key);
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
                Debug.Log("[Chronicles] Catalog validation passed: 40 hero kits, five UR Light/Dark realm pairs and 60 story stages. This is NOT a Unity/Android playtest.");
            else Debug.LogError("[Chronicles] Catalog validation failed with " + errors + " issues.");
        }
    }
}
#endif
