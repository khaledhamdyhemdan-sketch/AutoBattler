using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace AutoBattler.Editor
{
    /// <summary>
    /// Automated scene setup script - run from Unity menu
    /// Checks for existing objects to avoid duplication
    /// </summary>
    public static class SceneSetupScript
    {
        [MenuItem("Auto Battler/Setup All Scenes")]
        public static void SetupAllScenes()
        {
            Debug.Log("[SceneSetup] Starting automated scene setup...");

            // Setup each scene
            SetupPersistentManagersScene();
            SetupMainMenuScene();
            SetupGameplayScene();
            SetupGameHUDScene();

            // Configure build settings
            ConfigureBuildSettings();

            Debug.Log("[SceneSetup] All scenes configured successfully!");
            EditorUtility.DisplayDialog("Scene Setup Complete", 
                "All scenes have been created and configured.\n\n" +
                "Next steps:\n" +
                "1. Open PersistentManagers scene\n" +
                "2. Configure SceneLoader with scene names\n" +
                "3. Create unit prefabs and stats\n" +
                "4. Press Play to test!", "OK");
        }

        /// <summary>
        /// Helper to find or create a GameObject with a required component
        /// </summary>
        private static T FindOrCreate<T>(string name) where T : Component
        {
            // First try to find existing object by name
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                var component = existing.GetComponent<T>();
                if (component != null)
                {
                    Debug.Log($"[SceneSetup] Found existing {name} with {typeof(T).Name}");
                    return component;
                }
                // Object exists but missing component - add it
                Debug.Log($"[SceneSetup] Adding {typeof(T).Name} to existing {name}");
                return existing.AddComponent<T>();
            }

            // Also search by component type in case it was renamed
            var byComponent = Object.FindFirstObjectByType<T>();
            if (byComponent != null)
            {
                Debug.Log($"[SceneSetup] Found existing {typeof(T).Name} on '{byComponent.gameObject.name}'");
                return byComponent;
            }

            // Create new
            Debug.Log($"[SceneSetup] Creating new {name} with {typeof(T).Name}");
            var newObj = new GameObject(name);
            return newObj.AddComponent<T>();
        }

        /// <summary>
        /// Helper to find or create a primitive GameObject
        /// </summary>
        private static GameObject FindOrCreatePrimitive(string name, PrimitiveType type)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                Debug.Log($"[SceneSetup] Found existing {name}");
                return existing;
            }

            Debug.Log($"[SceneSetup] Creating new {name}");
            var newObj = GameObject.CreatePrimitive(type);
            newObj.name = name;
            return newObj;
        }

        /// <summary>
        /// Ensures a PanelSettings asset exists at the given path
        /// </summary>
        private static PanelSettings EnsurePanelSettings(string path)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (settings != null)
            {
                Debug.Log($"[SceneSetup] Using existing PanelSettings at {path}");
                return settings;
            }

            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                var parts = directory.Replace("\\", "/").Split('/');
                var currentPath = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var newPath = currentPath + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = newPath;
                }
            }

            Debug.Log($"[SceneSetup] Creating new PanelSettings at {path}");
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            AssetDatabase.CreateAsset(panelSettings, path);
            return panelSettings;
        }

        private static void SetupPersistentManagersScene()
        {
            string scenePath = "Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find or create managers
            var gameManager = FindOrCreate<AutoBattler.Core.GameManager>("GameManager");
            var combatManager = FindOrCreate<AutoBattler.Core.CombatManager>("CombatManager");
            var shopManager = FindOrCreate<AutoBattler.Core.ShopManager>("ShopManager");
            var grid = FindOrCreate<AutoBattler.Core.BattlefieldGrid>("BattlefieldGrid");
            var sceneLoader = FindOrCreate<AutoBattler.Core.SceneLoader>("SceneLoader");

            // Auto-assign references using SerializedObject
            SerializedObject so = new SerializedObject(gameManager);
            var shopProp = so.FindProperty("shopManager");
            var combatProp = so.FindProperty("combatManager");
            var gridProp = so.FindProperty("battlefieldGrid");
            
            if (shopProp != null) shopProp.objectReferenceValue = shopManager;
            if (combatProp != null) combatProp.objectReferenceValue = combatManager;
            if (gridProp != null) gridProp.objectReferenceValue = grid;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] PersistentManagers scene configured");
        }

        private static void SetupMainMenuScene()
        {
            string scenePath = "Assets/Scenes/MainMenu.unity/MainMenu.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find or create Camera
            var camera = FindOrCreate<Camera>("Main Camera");
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            // Find or create Light
            var light = FindOrCreate<Light>("Directional Light");
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Find or create UI Document for MainMenuUI
            var mainMenuUI = FindOrCreate<AutoBattler.UI.MainMenuUI>("MainMenuUI");
            var uiDoc = mainMenuUI.GetComponent<UIDocument>();
            if (uiDoc == null)
                uiDoc = mainMenuUI.gameObject.AddComponent<UIDocument>();

            // Ensure PanelSettings
            string panelSettingsPath = "Assets/Resources/Settings/MainMenuPanelSettings.asset";
            var settings = EnsurePanelSettings(panelSettingsPath);
            uiDoc.panelSettings = settings;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] MainMenu scene configured");
        }

        private static void SetupGameplayScene()
        {
            string scenePath = "Assets/Scenes/Gameplay.unity/Gameplay.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find or create Camera
            var camera = FindOrCreate<Camera>("Main Camera");
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = new Vector3(0, 10, -10);
            camera.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            // Find or create Directional Light
            var light = FindOrCreate<Light>("Directional Light");
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Find or create Ground Plane
            var groundObj = FindOrCreatePrimitive("Ground", PrimitiveType.Plane);
            groundObj.transform.position = Vector3.zero;
            groundObj.transform.localScale = new Vector3(5f, 1f, 5f);

            // Find or create UnitSpawner
            FindOrCreate<AutoBattler.Core.UnitSpawner>("UnitSpawner");

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] Gameplay scene configured");
        }

        private static void SetupGameHUDScene()
        {
            string scenePath = "Assets/Scenes/GameHUD.unity/GameHUD.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Ensure PanelSettings first
            string panelSettingsPath = "Assets/Resources/Settings/GameHUDPanelSettings.asset";
            var settings = EnsurePanelSettings(panelSettingsPath);

            // Find or create GameHUDUI
            var hudUI = FindOrCreate<AutoBattler.UI.GameHUDUI>("GameHUDUI");
            var uiDoc = hudUI.GetComponent<UIDocument>();
            if (uiDoc == null)
                uiDoc = hudUI.gameObject.AddComponent<UIDocument>();
            uiDoc.panelSettings = settings;
            uiDoc.sortingOrder = 100;

            // Ensure GameOverUI is on the same object
            if (hudUI.GetComponent<AutoBattler.UI.GameOverUI>() == null)
                hudUI.gameObject.AddComponent<AutoBattler.UI.GameOverUI>();

            // Find or create ShopUI as child
            Transform shopTransform = hudUI.transform.Find("ShopUI");
            AutoBattler.UI.ShopUIController shopController;
            
            if (shopTransform != null)
            {
                shopController = shopTransform.GetComponent<AutoBattler.UI.ShopUIController>();
                if (shopController == null)
                    shopController = shopTransform.gameObject.AddComponent<AutoBattler.UI.ShopUIController>();
                Debug.Log("[SceneSetup] Found existing ShopUI child");
            }
            else
            {
                // Also check if ShopUI exists but not as a child
                var existingShop = Object.FindFirstObjectByType<AutoBattler.UI.ShopUIController>();
                if (existingShop != null)
                {
                    // Re-parent it
                    existingShop.transform.SetParent(hudUI.transform);
                    shopController = existingShop;
                    Debug.Log("[SceneSetup] Re-parented existing ShopUI");
                }
                else
                {
                    // Create new
                    Debug.Log("[SceneSetup] Creating new ShopUI child");
                    var shopObj = new GameObject("ShopUI");
                    shopObj.transform.SetParent(hudUI.transform);
                    shopController = shopObj.AddComponent<AutoBattler.UI.ShopUIController>();
                }
            }

            // Configure ShopUI's UIDocument
            var shopDoc = shopController.GetComponent<UIDocument>();
            if (shopDoc == null)
                shopDoc = shopController.gameObject.AddComponent<UIDocument>();
            shopDoc.panelSettings = settings;
            shopDoc.sortingOrder = 110;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] GameHUD scene configured");
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/UnitSelection.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity/Gameplay.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameHUD.unity/GameHUD.unity", true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[SceneSetup] Build settings configured");
        }
    }
}
