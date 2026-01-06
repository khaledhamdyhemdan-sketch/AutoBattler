using UnityEngine;
using UnityEditor;
using AutoBattler.Core;
using UnityEngine.UIElements;

namespace AutoBattler.Editor
{
    /// <summary>
    /// Editor utility for quick combat scene setup
    /// </summary>
    public class CombatSceneSetupEditor : EditorWindow
    {
        private int playerUnitCount = 3;
        private int enemyUnitCount = 3;
        private bool autoStartCombat = false;

        [MenuItem("Auto Battler/Combat Scene Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<CombatSceneSetupEditor>("Combat Setup");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Combat Scene Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard will help you set up a complete combat scene with managers, UI, and test units.",
                MessageType.Info);
            GUILayout.Space(10);

            // Setup Section
            GUILayout.Label("Scene Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Create All Manager Objects", GUILayout.Height(30)))
            {
                CreateManagerObjects();
            }

            if (GUILayout.Button("Create Combat UI", GUILayout.Height(30)))
            {
                CreateCombatUI();
            }

            if (GUILayout.Button("Create Quick Battle Setup", GUILayout.Height(30)))
            {
                CreateQuickBattleSetup();
            }

            GUILayout.Space(10);

            // Battle Configuration Section
            GUILayout.Label("Battle Configuration", EditorStyles.boldLabel);
            playerUnitCount = EditorGUILayout.IntField("Player Units", playerUnitCount);
            enemyUnitCount = EditorGUILayout.IntField("Enemy Units", enemyUnitCount);
            autoStartCombat = EditorGUILayout.Toggle("Auto Start Combat", autoStartCombat);

            GUILayout.Space(10);

            if (GUILayout.Button("Setup Complete Scene", GUILayout.Height(40)))
            {
                SetupCompleteScene();
            }

            GUILayout.Space(20);

            // Testing Section
            GUILayout.Label("Testing (Play Mode Only)", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            
            if (GUILayout.Button("Spawn Test Units", GUILayout.Height(30)))
            {
                SpawnTestUnits();
            }

            if (GUILayout.Button("Start Combat Now", GUILayout.Height(30)))
            {
                StartCombatNow();
            }

            if (GUILayout.Button("Clear All Units", GUILayout.Height(30)))
            {
                ClearAllUnits();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);

            // Information Section
            EditorGUILayout.HelpBox(
                "Quick Reference:\n" +
                "1. Click 'Setup Complete Scene' to create everything\n" +
                "2. Enter Play Mode\n" +
                "3. Units will spawn automatically\n" +
                "4. Click Fight button or press Space to start combat",
                MessageType.None);
        }

        private void CreateManagerObjects()
        {
            // Create root Managers object
            GameObject managersRoot = GameObject.Find("Managers");
            if (managersRoot == null)
            {
                managersRoot = new GameObject("Managers");
                Undo.RegisterCreatedObjectUndo(managersRoot, "Create Managers");
            }

            // Create GameManager
            GameObject gameManagerObj = GameObject.Find("GameManager");
            if (gameManagerObj == null)
            {
                gameManagerObj = new GameObject("GameManager");
                gameManagerObj.transform.SetParent(managersRoot.transform);
                var gameManager = gameManagerObj.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create GameManager");
                
                Debug.Log("[Setup] GameManager created");
            }

            // Create CombatManager
            GameObject combatManagerObj = GameObject.Find("CombatManager");
            if (combatManagerObj == null)
            {
                combatManagerObj = new GameObject("CombatManager");
                combatManagerObj.transform.SetParent(managersRoot.transform);
                combatManagerObj.AddComponent<CombatManager>();
                Undo.RegisterCreatedObjectUndo(combatManagerObj, "Create CombatManager");
                
                Debug.Log("[Setup] CombatManager created");
            }

            // Create UnitSpawner
            GameObject spawnerObj = GameObject.Find("UnitSpawner");
            if (spawnerObj == null)
            {
                spawnerObj = new GameObject("UnitSpawner");
                spawnerObj.transform.SetParent(managersRoot.transform);
                spawnerObj.AddComponent<UnitSpawner>();
                Undo.RegisterCreatedObjectUndo(spawnerObj, "Create UnitSpawner");
                
                Debug.Log("[Setup] UnitSpawner created");
            }

            // Create LootManager
            GameObject lootManagerObj = GameObject.Find("LootManager");
            if (lootManagerObj == null)
            {
                lootManagerObj = new GameObject("LootManager");
                lootManagerObj.transform.SetParent(managersRoot.transform);
                lootManagerObj.AddComponent<LootManager>();
                Undo.RegisterCreatedObjectUndo(lootManagerObj, "Create LootManager");
                Debug.Log("[Setup] LootManager created");
            }

            // Create UnitSelectionManager
            GameObject selectionManagerObj = GameObject.Find("UnitSelectionManager");
            if (selectionManagerObj == null)
            {
                selectionManagerObj = new GameObject("UnitSelectionManager");
                selectionManagerObj.transform.SetParent(managersRoot.transform);
                selectionManagerObj.AddComponent<UnitSelectionManager>();
                Undo.RegisterCreatedObjectUndo(selectionManagerObj, "Create UnitSelectionManager");
                Debug.Log("[Setup] UnitSelectionManager created");
            }

            EditorUtility.DisplayDialog("Success", "Manager objects created successfully!", "OK");
        }

        private void CreateCombatUI()
        {
            // Note: Combat UI functionality is now handled by GameHUDUI
            GameObject uiObj = GameObject.Find("GameHUD");
            if (uiObj == null)
            {
                uiObj = new GameObject("GameHUD");
                
                var uiDocument = uiObj.AddComponent<UIDocument>();
                var gameHUD = uiObj.AddComponent<UI.GameHUDUI>();
                
                // Set up PanelSettings if available
                var panelSettings = FindOrCreatePanelSettings();
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                }

                Undo.RegisterCreatedObjectUndo(uiObj, "Create GameHUD");
                
                Debug.Log("[Setup] GameHUD created");
                EditorUtility.DisplayDialog("Success", "GameHUD created successfully!", "OK");
            }
            else
            {
                Debug.LogWarning("[Setup] GameHUD already exists");
            }
        }

        private void CreateQuickBattleSetup()
        {
            GameObject setupObj = GameObject.Find("BattleSetup");
            if (setupObj == null)
            {
                setupObj = new GameObject("BattleSetup");
                var setup = setupObj.AddComponent<Testing.QuickBattleSetup>();
                
                // Use reflection to set private fields
                var setupType = typeof(Testing.QuickBattleSetup);
                
                var playerCountField = setupType.GetField("playerUnitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (playerCountField != null) playerCountField.SetValue(setup, playerUnitCount);
                
                var enemyCountField = setupType.GetField("enemyUnitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enemyCountField != null) enemyCountField.SetValue(setup, enemyUnitCount);
                
                var autoStartField = setupType.GetField("autoStartCombat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (autoStartField != null) autoStartField.SetValue(setup, autoStartCombat);

                Undo.RegisterCreatedObjectUndo(setupObj, "Create Battle Setup");
                
                Debug.Log("[Setup] Quick Battle Setup created");
                EditorUtility.DisplayDialog("Success", "Quick Battle Setup created successfully!", "OK");
            }
            else
            {
                Debug.LogWarning("[Setup] Battle Setup already exists");
            }
        }

        private void SetupCompleteScene()
        {
            CreateManagerObjects();
            CreateCombatUI();
            CreateQuickBattleSetup();
            
            // Create camera if needed
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                var cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                cam.transform.position = new Vector3(0, 10, -10);
                cam.transform.rotation = Quaternion.Euler(45, 0, 0);
                Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
                
                Debug.Log("[Setup] Camera created");
            }

            // Create directional light if needed
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
                Undo.RegisterCreatedObjectUndo(lightObj, "Create Light");
                
                Debug.Log("[Setup] Directional Light created");
            }

            // Create ground plane
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj == null)
            {
                groundObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                groundObj.name = "Ground";
                groundObj.transform.position = Vector3.zero;
                groundObj.transform.localScale = new Vector3(5, 1, 5);
                Undo.RegisterCreatedObjectUndo(groundObj, "Create Ground");
                
                Debug.Log("[Setup] Ground plane created");
            }

            EditorUtility.DisplayDialog("Complete!", 
                "Combat scene setup complete!\n\n" +
                "Next steps:\n" +
                "1. Configure UnitSpawner with unit prefab and stats\n" +
                "2. Enter Play Mode\n" +
                "3. Units will spawn automatically\n" +
                "4. Click Fight button to start combat", 
                "OK");
        }

        private void SpawnTestUnits()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SetupBattle(playerUnitCount, enemyUnitCount);
                Debug.Log($"[Setup] Spawned {playerUnitCount} player units and {enemyUnitCount} enemy units");
            }
            else
            {
                Debug.LogError("[Setup] UnitSpawner not found in scene!");
            }
        }

        private void StartCombatNow()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Combat);
                Debug.Log("[Setup] Combat started");
            }
            else
            {
                Debug.LogError("[Setup] GameManager not found in scene!");
            }
        }

        private void ClearAllUnits()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.ClearAllUnits();
                Debug.Log("[Setup] All units cleared");
            }
        }

        private PanelSettings FindOrCreatePanelSettings()
        {
            // Try to find existing PanelSettings
            string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            }

            // Create new PanelSettings
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            
            string settingsPath = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            string assetPath = $"{settingsPath}/PanelSettings.asset";
            AssetDatabase.CreateAsset(panelSettings, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Setup] Created PanelSettings at {assetPath}");
            
            return panelSettings;
        }
    }
}
