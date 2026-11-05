#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EgyptianArena.Editor
{
    public static class EgyptianArenaSceneCreator
    {
        [MenuItem("Tools/Egyptian Arena/Create Playable 5v5 Scene")]
        public static void CreatePlayableScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Egyptian Arena - 5v5 Demo").AddComponent<EgyptianArenaDemo>();
            const string path = "Assets/EgyptianArena/EgyptianArena_Playable.unity";
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Object sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EditorGUIUtility.PingObject(sceneAsset);
            Debug.Log("[EgyptianArena] Created playable demo scene: " + path + ". Press Play.");
        }
    }
}
#endif
