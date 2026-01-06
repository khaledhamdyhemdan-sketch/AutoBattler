using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using AutoBattler.Core;
using AutoBattler.Data;

namespace AutoBattler.Editor
{
    /// <summary>
    /// Editor utility to automatically assign references to RoguelikeSystemSetup
    /// </summary>
    public static class SetupRoguelikeReferences
    {
        [MenuItem("Auto Battler/Setup Roguelike References")]
        public static void SetupReferences()
        {
            // Find the RoguelikeSystemSetup in the scene
            var setup = Object.FindFirstObjectByType<RoguelikeSystemSetup>();
            if (setup == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] RoguelikeSystemSetup not found in scene! Make sure PersistentManagers scene is loaded.");
                return;
            }

            // Load assets
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/Settings/GameHUDPanelSettings.asset");
            var draftUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Draft/DraftUI.uxml");
            var lotteryUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Lottery/LotteryUI.uxml");
            var equipmentDB = AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>("Assets/Data/Equipment/EquipmentDatabase.asset");

            // Verify all assets loaded
            if (panelSettings == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] Failed to load GameHUDPanelSettings.asset");
                return;
            }
            if (draftUXML == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] Failed to load DraftUI.uxml");
                return;
            }
            if (lotteryUXML == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] Failed to load LotteryUI.uxml");
                return;
            }
            if (equipmentDB == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] Failed to load EquipmentDatabase.asset");
                return;
            }

            // Use reflection to set private serialized fields
            var setupType = typeof(RoguelikeSystemSetup);
            
            var panelSettingsField = setupType.GetField("panelSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var draftUXMLField = setupType.GetField("draftUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lotteryUXMLField = setupType.GetField("lotteryUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var equipmentDBField = setupType.GetField("equipmentDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (panelSettingsField == null || draftUXMLField == null || lotteryUXMLField == null || equipmentDBField == null)
            {
                Debug.LogError("[SetupRoguelikeReferences] Failed to find serialized fields via reflection. Fields may have been renamed.");
                return;
            }

            // Set the fields
            panelSettingsField.SetValue(setup, panelSettings);
            draftUXMLField.SetValue(setup, draftUXML);
            lotteryUXMLField.SetValue(setup, lotteryUXML);
            equipmentDBField.SetValue(setup, equipmentDB);

            // Mark the object as dirty so Unity saves the changes
            EditorUtility.SetDirty(setup);
            
            // Save the scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(setup.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[SetupRoguelikeReferences] ✓ Successfully assigned all references to RoguelikeSystemSetup!");
            Debug.Log($"  - Panel Settings: {panelSettings.name}");
            Debug.Log($"  - Draft UXML: {draftUXML.name}");
            Debug.Log($"  - Lottery UXML: {lotteryUXML.name}");
            Debug.Log($"  - Equipment DB: {equipmentDB.name} ({equipmentDB.allEquipment.Count} items)");
        }

        [MenuItem("Auto Battler/Verify Roguelike Setup")]
        public static void VerifySetup()
        {
            var setup = Object.FindFirstObjectByType<RoguelikeSystemSetup>();
            if (setup == null)
            {
                Debug.LogError("[VerifySetup] RoguelikeSystemSetup not found!");
                return;
            }

            var setupType = typeof(RoguelikeSystemSetup);
            var panelSettingsField = setupType.GetField("panelSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var draftUXMLField = setupType.GetField("draftUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lotteryUXMLField = setupType.GetField("lotteryUXML", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var equipmentDBField = setupType.GetField("equipmentDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var panelSettings = panelSettingsField?.GetValue(setup);
            var draftUXML = draftUXMLField?.GetValue(setup);
            var lotteryUXML = lotteryUXMLField?.GetValue(setup);
            var equipmentDB = equipmentDBField?.GetValue(setup);

            Debug.Log("=== Roguelike Setup Verification ===");
            Debug.Log($"Panel Settings: {(panelSettings != null ? "✓ Assigned" : "✗ Missing")}");
            Debug.Log($"Draft UXML: {(draftUXML != null ? "✓ Assigned" : "✗ Missing")}");
            Debug.Log($"Lottery UXML: {(lotteryUXML != null ? "✓ Assigned" : "✗ Missing")}");
            Debug.Log($"Equipment DB: {(equipmentDB != null ? "✓ Assigned" : "✗ Missing")}");

            bool allAssigned = panelSettings != null && draftUXML != null && lotteryUXML != null && equipmentDB != null;
            if (allAssigned)
            {
                Debug.Log("✓ All references properly assigned!");
            }
            else
            {
                Debug.LogWarning("⚠ Some references are missing. Run 'Auto Battler/Setup Roguelike References' to fix.");
            }
        }
    }
}
