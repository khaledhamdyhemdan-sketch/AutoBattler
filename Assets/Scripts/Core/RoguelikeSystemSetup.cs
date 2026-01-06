using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Automatic setup script for roguelike progression systems.
    /// Add this to your persistent managers scene to initialize all roguelike systems.
    /// 
    /// SETUP INSTRUCTIONS:
    /// 1. In your persistent managers scene, create an empty GameObject named "RoguelikeSystemsManager"
    /// 2. Add this script as a component
    /// 3. That's it! All systems will auto-initialize on scene load.
    /// 
    /// This will create:
    /// - RunManager (tracks current run progress)
    /// - UnlockManager (persistent meta-progression unlocks)
    /// - AchievementSystem (achievements with observer pattern)
    /// 
    /// These will automatically hook into GameManager's phase system with ZERO additional setup needed.
    /// </summary>
    public class RoguelikeSystemSetup : MonoBehaviour
    {
        [Header("Systems Configuration")]
        [SerializeField] private bool createRunManager = true;
        [SerializeField] private bool createUnlockManager = true;
        [SerializeField] private bool createAchievementSystem = true;
        [SerializeField] private bool createRoguelikeManager = true;

        [Header("UI Configuration")]
        [SerializeField] private bool createDraftUI = true;
        [SerializeField] private bool createLotteryUI = true;
        [SerializeField] private UnityEngine.UIElements.PanelSettings panelSettings;
        [SerializeField] private UnityEngine.UIElements.VisualTreeAsset draftUXML;
        [SerializeField] private UnityEngine.UIElements.VisualTreeAsset lotteryUXML;

        [Header("Data References")]
        [SerializeField] private Data.EquipmentDatabaseSO equipmentDatabase;
        
        private void Start()
        {
            // Ensure GameManager exists first
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[RoguelikeSystemSetup] GameManager not found in Start. Waiting for next frame...");
                StartCoroutine(WaitForGameManager());
                return;
            }
            
            InitializeSystems();
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
            {
                yield return null;
            }
            InitializeSystems();
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "GameHUD")
            {
                InitializeHUDUI(scene);
            }
        }

        private void InitializeHUDUI(UnityEngine.SceneManagement.Scene hudScene)
        {
            if (createDraftUI)
            {
                // Check if already exists in the scene to prevent duplicates (though uncommon with fresh load)
                bool exists = false;
                foreach (var root in hudScene.GetRootGameObjects())
                {
                    if (root.GetComponent<UI.DraftUIController>()) { exists = true; break; }
                }

                if (!exists)
                {
                    var go = new GameObject("DraftUI");
                    var doc = go.AddComponent<UnityEngine.UIElements.UIDocument>();
                    doc.panelSettings = panelSettings;
                    doc.visualTreeAsset = draftUXML;
                    doc.sortingOrder = 100; // Ensure it renders on top of HUD
                    go.AddComponent<UI.DraftUIController>();
                    
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, hudScene);
                    Debug.Log("[RoguelikeSystemSetup] Injected DraftUI into GameHUD");
                }
            }

            if (createLotteryUI)
            {
                bool exists = false;
                foreach (var root in hudScene.GetRootGameObjects())
                {
                    if (root.GetComponent<UI.LotteryUIController>()) { exists = true; break; }
                }

                if (!exists)
                {
                    var go = new GameObject("LotteryUI");
                    var doc = go.AddComponent<UnityEngine.UIElements.UIDocument>();
                    doc.panelSettings = panelSettings;
                    doc.visualTreeAsset = lotteryUXML;
                    doc.sortingOrder = 200; // Ensure it renders on top of everything
                    go.AddComponent<UI.LotteryUIController>();
                    
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, hudScene);
                    Debug.Log("[RoguelikeSystemSetup] Injected LotteryUI into GameHUD");
                }
            }
        }

        private void InitializeSystems()
        {
            // Create RunManager if needed
            if (createRunManager && RunManager.Instance == null)
            {
                var runManagerGO = new GameObject("RunManager");
                var runManager = runManagerGO.AddComponent<RunManager>();
                Debug.Log("[RoguelikeSystemSetup] Created RunManager");
            }
            
            // Create UnlockManager if needed
            if (createUnlockManager && UnlockManager.Instance == null)
            {
                var unlockManagerGO = new GameObject("UnlockManager");
                var unlockManager = unlockManagerGO.AddComponent<UnlockManager>();
                Debug.Log("[RoguelikeSystemSetup] Created UnlockManager");
            }
            
            // Create AchievementSystem if needed
            if (createAchievementSystem && AchievementSystem.Instance == null)
            {
                var achievementGO = new GameObject("AchievementSystem");
                var achievement = achievementGO.AddComponent<AchievementSystem>();
                Debug.Log("[RoguelikeSystemSetup] Created AchievementSystem");
            }

            // Create RoguelikeManager if needed
            if (createRoguelikeManager && RoguelikeManager.Instance == null)
            {
                var roguelikeManagerGO = new GameObject("RoguelikeManager");
                var roguelikeManager = roguelikeManagerGO.AddComponent<RoguelikeManager>();
                
                // Assign Equipment Database via reflection or public method if available
                var dbField = typeof(RoguelikeManager).GetField("equipmentDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbField != null && equipmentDatabase != null)
                {
                    dbField.SetValue(roguelikeManager, equipmentDatabase);
                }

                Debug.Log("[RoguelikeSystemSetup] Created RoguelikeManager");
            }

            Debug.Log("[RoguelikeSystemSetup] Roguelike progression systems initialized successfully!");
        }
    }
}
