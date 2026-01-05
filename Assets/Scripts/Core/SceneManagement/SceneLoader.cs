using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages scene loading and transitions with additive scene loading
    /// Handles persistent managers and UI scenes
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string unitSelectionScene = "UnitSelection";
        [SerializeField] private string gameplayScene = "Gameplay";
        [SerializeField] private string gameHUDScene = "GameHUD";

        [Header("Settings")]
        [SerializeField] private float minLoadTime = 0.5f;

        // Events
        public event System.Action<string> OnSceneLoadStarted;
        public event System.Action<string> OnSceneLoadComplete;
        public event System.Action<float> OnLoadProgress;

        private Scene currentGameplayScene;
        private Scene currentUIScene;
        private bool isLoading = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Debug.Log("[SceneLoader] Initialized");
        }

        #endregion

        #region Scene Loading

        /// <summary>
        /// Load the main menu (unloads gameplay and HUD)
        /// </summary>
        public void LoadMainMenu()
        {
            if (isLoading) return;
            StartCoroutine(LoadMainMenuCoroutine());
        }

        /// <summary>
        /// Load unit selection screen (unloads main menu)
        /// </summary>
        public void LoadUnitSelection()
        {
            if (isLoading) return;
            StartCoroutine(LoadUnitSelectionCoroutine());
        }

        /// <summary>
        /// Load gameplay and HUD (unloads unit selection)
        /// </summary>
        public void LoadGameplay()
        {
            if (isLoading) return;
            StartCoroutine(LoadGameplayCoroutine());
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[SceneLoader] Quitting game");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Coroutines

        private IEnumerator LoadMainMenuCoroutine()
        {
            isLoading = true;
            OnSceneLoadStarted?.Invoke(mainMenuScene);

            // Unload gameplay scenes if loaded
            if (currentGameplayScene.IsValid())
            {
                yield return UnloadSceneAsync(currentGameplayScene);
            }

            if (currentUIScene.IsValid())
            {
                yield return UnloadSceneAsync(currentUIScene);
            }

            // Load main menu
            yield return LoadSceneAdditiveAsync(mainMenuScene, true);
            currentUIScene = SceneManager.GetSceneByName(mainMenuScene);

            // Update game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.MainMenu);
            }

            isLoading = false;
            OnSceneLoadComplete?.Invoke(mainMenuScene);

            Debug.Log("[SceneLoader] Main Menu loaded");
        }

        private IEnumerator LoadUnitSelectionCoroutine()
        {
            isLoading = true;
            OnSceneLoadStarted?.Invoke(unitSelectionScene);

            // Unload main menu if loaded
            Scene mainMenu = SceneManager.GetSceneByName(mainMenuScene);
            if (mainMenu.IsValid())
            {
                yield return UnloadSceneAsync(mainMenu);
            }

            // Load unit selection scene
            yield return LoadSceneAdditiveAsync(unitSelectionScene, true);
            currentUIScene = SceneManager.GetSceneByName(unitSelectionScene);

            // Update game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Selection);
            }

            isLoading = false;
            OnSceneLoadComplete?.Invoke(unitSelectionScene);

            Debug.Log("[SceneLoader] Unit Selection loaded");
        }

        private IEnumerator LoadGameplayCoroutine()
        {
            isLoading = true;
            OnSceneLoadStarted?.Invoke(gameplayScene);

            // Unload unit selection if loaded
            Scene selection = SceneManager.GetSceneByName(unitSelectionScene);
            if (selection.IsValid())
            {
                yield return UnloadSceneAsync(selection);
            }

            // Load gameplay scene
            yield return LoadSceneAdditiveAsync(gameplayScene, false);
            currentGameplayScene = SceneManager.GetSceneByName(gameplayScene);

            // Load HUD scene
            yield return LoadSceneAdditiveAsync(gameHUDScene, true);
            currentUIScene = SceneManager.GetSceneByName(gameHUDScene);

            // Set gameplay as active scene
            if (currentGameplayScene.IsValid())
            {
                SceneManager.SetActiveScene(currentGameplayScene);
            }

            // Spawn selected units
            SpawnSelectedUnits();

            // Update game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Preparation);
            }

            isLoading = false;
            OnSceneLoadComplete?.Invoke(gameplayScene);

            Debug.Log("[SceneLoader] Gameplay loaded");
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName, bool setActive)
        {
            float startTime = Time.time;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                OnLoadProgress?.Invoke(progress);

                // Wait for minimum load time
                if (operation.progress >= 0.9f && (Time.time - startTime) >= minLoadTime)
                {
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (setActive && loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }

            OnLoadProgress?.Invoke(1f);
        }

        private IEnumerator UnloadSceneAsync(Scene scene)
        {
            if (!scene.IsValid()) yield break;

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return Resources.UnloadUnusedAssets();
        }

        #endregion

        #region Helper Methods

        public bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public bool IsLoading => isLoading;

        private void SpawnSelectedUnits()
        {
            // Get selected squad from UnitSelectionManager
            if (UnitSelectionManager.Instance == null)
            {
                Debug.LogError("[SceneLoader] UnitSelectionManager not found!");
                return;
            }

            var selectedSquad = UnitSelectionManager.Instance.GetSelectedSquad();
            if (selectedSquad == null || selectedSquad.Count == 0)
            {
                Debug.LogWarning("[SceneLoader] No units selected! Using default units.");
                // Spawn default player units if none selected
                if (UnitSpawner.Instance != null)
                {
                    UnitSpawner.Instance.SpawnPlayerUnits(3);
                }
                return;
            }

            // Spawn each selected unit on the player's side
            if (UnitSpawner.Instance != null)
            {
                foreach (var unitStats in selectedSquad)
                {
                    if (unitStats != null)
                    {
                        UnitSpawner.Instance.SpawnPlayerUnit(unitStats);
                    }
                }
                Debug.Log($"[SceneLoader] Spawned {selectedSquad.Count} player units");
            }
        }

        #endregion
    }
}
