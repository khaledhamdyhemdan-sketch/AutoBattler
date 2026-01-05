using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AutoBattler.Editor
{
    public static class SceneNavigator
    {
        [MenuItem("Auto Battler/Open Persistent Managers")]
        public static void OpenPersistentManagers()
        {
            string scenePath = "Assets/Scenes/PersistentManagers.unity/PersistentManagers.unity";
            
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log($"[SceneNavigator] Opened {scenePath}");
            }
        }
    }
}
