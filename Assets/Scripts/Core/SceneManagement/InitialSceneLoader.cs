using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Automatically loads the MainMenu scene when the PersistentManagers scene is loaded.
    /// This should be attached to a GameObject in the PersistentManagers scene.
    /// </summary>
    public class InitialSceneLoader : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("[InitialSceneLoader] Loading MainMenu scene");
            SceneLoader.Instance.LoadMainMenu();
        }
    }
}
