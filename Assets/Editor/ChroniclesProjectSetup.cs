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
            if (WorldsCatalog.Heroes.Length != 30)
            {
                Debug.LogError("[Chronicles] Expected 30 named heroes."); errors++;
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
            int rCount = 0, srCount = 0, ssrCount = 0, special = 0;
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
            {
                if (hero.element != HeroElements.For(hero.id))
                {
                    Debug.LogError("[Chronicles] Hero element is not initialized: " + hero.id);
                    errors++;
                }
                if (HeroElements.IsSpecial(hero))
                {
                    special++;
                    if (hero.rarity != Rarity.SSR)
                    {
                        Debug.LogError("[Chronicles] Special hero must be SSR: " + hero.id);
                        errors++;
                    }
                }
                if (hero.rarity == Rarity.R) rCount++;
                else if (hero.rarity == Rarity.SR) srCount++;
                else if (hero.rarity == Rarity.SSR) ssrCount++;
                else { Debug.LogError("[Chronicles] Future ranks must stay locked."); errors++; }
            }
            if (rCount != 2 || srCount != 8 || ssrCount != 20 || special != 4)
            {
                Debug.LogError("[Chronicles] R/SR/SSR or special element roster allocation invalid.");
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
                Debug.Log("[Chronicles] Catalog validation passed: 30 hero kits, 6 worlds, 60 stages and narrative beats. This is NOT a Unity/Android playtest.");
            else Debug.LogError("[Chronicles] Catalog validation failed with " + errors + " issues.");
        }
    }
}
#endif
