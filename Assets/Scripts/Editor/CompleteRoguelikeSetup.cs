using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Editor
{
    /// <summary>
    /// Complete scene setup for Roguelike system
    /// </summary>
    public static class CompleteRoguelikeSetup
    {
        [MenuItem("Auto Battler/Complete Roguelike Setup", priority = 0)]
        public static void PerformCompleteSetup()
        {
            Debug.Log("=== Starting Complete Roguelike Setup ===");

            // 1. Ensure PersistentManagers scene is active
            Scene persistentScene = SceneManager.GetSceneByName("PersistentManagers");
            if (!persistentScene.isLoaded)
            {
                persistentScene = EditorSceneManager.OpenScene("Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity", OpenSceneMode.Single);
            }
            EditorSceneManager.SetActiveScene(persistentScene);

            // 2. Find or create Roguelike System Setup GameObject
            GameObject setupGO = GameObject.Find("Roguelike System Setup");
            if (setupGO == null)
            {
                setupGO = new GameObject("Roguelike System Setup");
                Undo.RegisterCreatedObjectUndo(setupGO, "Create Roguelike System Setup");
                Debug.Log("✓ Created 'Roguelike System Setup' GameObject");
            }
            else
            {
                Debug.Log("✓ Found existing 'Roguelike System Setup' GameObject");
            }

            // 3. Add RoguelikeSystemSetup component if not present
            var setupComponent = setupGO.GetComponent<RoguelikeSystemSetup>();
            if (setupComponent == null)
            {
                setupComponent = setupGO.AddComponent<RoguelikeSystemSetup>();
                Undo.RegisterCreatedObjectUndo(setupComponent, "Add RoguelikeSystemSetup");
                Debug.Log("✓ Added RoguelikeSystemSetup component");
            }

            // 4. Add RoguelikeResourceLoader for runtime fallback
            var resourceLoader = setupGO.GetComponent<RoguelikeResourceLoader>();
            if (resourceLoader == null)
            {
                resourceLoader = setupGO.AddComponent<RoguelikeResourceLoader>();
                Undo.RegisterCreatedObjectUndo(resourceLoader, "Add RoguelikeResourceLoader");
                Debug.Log("✓ Added RoguelikeResourceLoader component");
            }

            // 5. Load and assign all required assets
            AssignReferences(setupComponent);

            // 6. Verify all managers exist
            VerifyManagers();

            // 7. Save the scene
            EditorSceneManager.MarkSceneDirty(persistentScene);
            EditorSceneManager.SaveScene(persistentScene);
            
            Debug.Log("=== Roguelike Setup Complete! ===");
            EditorUtility.DisplayDialog("Setup Complete", 
                "Roguelike system has been fully configured!\n\n" +
                "✓ GameObject created\n" +
                "✓ Components attached\n" +
                "✓ References assigned\n" +
                "✓ Scene saved", "OK");
        }

        private static void AssignReferences(RoguelikeSystemSetup setup)
        {
            // Load assets from various locations
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/Settings/GameHUDPanelSettings.asset");
            var draftUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Draft/DraftUI.uxml");
            var lotteryUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Lottery/LotteryUI.uxml");
            var equipmentDB = AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>("Assets/Data/Equipment/EquipmentDatabase.asset");

            // Fallback to Resources locations
            if (draftUXML == null)
                draftUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/UI/Draft/DraftUI.uxml");
            if (lotteryUXML == null)
                lotteryUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/UI/Lottery/LotteryUI.uxml");
            if (equipmentDB == null)
                equipmentDB = AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>("Assets/Resources/ScriptableObjects/EquipmentDatabase.asset");

            // Verify all assets loaded
            bool allLoaded = true;
            if (panelSettings == null) { Debug.LogError("✗ Failed to load GameHUDPanelSettings"); allLoaded = false; }
            if (draftUXML == null) { Debug.LogError("✗ Failed to load DraftUI.uxml"); allLoaded = false; }
            if (lotteryUXML == null) { Debug.LogError("✗ Failed to load LotteryUI.uxml"); allLoaded = false; }
            if (equipmentDB == null) { Debug.LogError("✗ Failed to load EquipmentDatabase"); allLoaded = false; }

            if (!allLoaded)
            {
                Debug.LogError("Some assets failed to load. Setup incomplete.");
                return;
            }

            // Use SerializedObject to properly set private fields
            SerializedObject so = new SerializedObject(setup);
            
            so.FindProperty("panelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("draftUXML").objectReferenceValue = draftUXML;
            so.FindProperty("lotteryUXML").objectReferenceValue = lotteryUXML;
            so.FindProperty("equipmentDatabase").objectReferenceValue = equipmentDB;
            
            so.ApplyModifiedProperties();

            Debug.Log($"✓ Panel Settings: {panelSettings.name}");
            Debug.Log($"✓ Draft UXML: {draftUXML.name}");
            Debug.Log($"✓ Lottery UXML: {lotteryUXML.name}");
            Debug.Log($"✓ Equipment DB: {equipmentDB.name} ({equipmentDB.allEquipment.Count} items)");
        }

        private static void VerifyManagers()
        {
            Debug.Log("\n--- Verifying Manager GameObjects ---");
            
            string[] requiredManagers = new string[]
            {
                "GameManager",
                "CombatManager",
                "ShopManager",
                "SceneLoader",
                "RunManager",
                "UnlockManager",
                "AchievementSystem",
                "Roguelike System Setup"
            };

            foreach (var managerName in requiredManagers)
            {
                var go = GameObject.Find(managerName);
                if (go != null)
                {
                    Debug.Log($"✓ {managerName}");
                }
                else
                {
                    Debug.LogWarning($"⚠ {managerName} not found!");
                }
            }
        }

        [MenuItem("Auto Battler/Verify Complete Setup", priority = 1)]
        public static void VerifyCompleteSetup()
        {
            Debug.Log("\n=== Verifying Roguelike Setup ===\n");

            // Find setup
            var setup = Object.FindFirstObjectByType<RoguelikeSystemSetup>();
            if (setup == null)
            {
                Debug.LogError("✗ RoguelikeSystemSetup component not found!");
                return;
            }

            // Check references using SerializedObject
            SerializedObject so = new SerializedObject(setup);
            
            var panelSettings = so.FindProperty("panelSettings").objectReferenceValue;
            var draftUXML = so.FindProperty("draftUXML").objectReferenceValue;
            var lotteryUXML = so.FindProperty("lotteryUXML").objectReferenceValue;
            var equipmentDB = so.FindProperty("equipmentDatabase").objectReferenceValue;

            Debug.Log($"Panel Settings: {(panelSettings != null ? "✓" : "✗")} {panelSettings?.name}");
            Debug.Log($"Draft UXML: {(draftUXML != null ? "✓" : "✗")} {draftUXML?.name}");
            Debug.Log($"Lottery UXML: {(lotteryUXML != null ? "✓" : "✗")} {lotteryUXML?.name}");
            Debug.Log($"Equipment DB: {(equipmentDB != null ? "✓" : "✗")} {equipmentDB?.name}");

            bool allValid = panelSettings != null && draftUXML != null && 
                           lotteryUXML != null && equipmentDB != null;

            VerifyManagers();

            if (allValid)
            {
                Debug.Log("\n✓✓✓ All references properly assigned! ✓✓✓");
                EditorUtility.DisplayDialog("Verification Success", 
                    "All roguelike references are properly configured!", "OK");
            }
            else
            {
                Debug.LogWarning("\n⚠ Some references missing! Run 'Complete Roguelike Setup'");
            }
        }
    }
}
