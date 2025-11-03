#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayPreloaderButton
{
	[MenuItem("Tools/▶ Play From Preloader", priority = 0)]
	public static void PlayFromPreloader()
	{
		
		/*const string sceneName = "preloader";

		var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
		if (guids.Length == 0)
		{
			EditorUtility.DisplayDialog("Preloader not found",
				$"Не найдена сцена '{sceneName}'.", "Ок");
			return;
		}

		var path = AssetDatabase.GUIDToAssetPath(guids[0]);
		var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
		if (scene == null)
		{
			EditorUtility.DisplayDialog("Error", "Не удалось загрузить сцену preloader.", "Ок");
			return;
		}

		EditorSceneManager.playModeStartScene = scene;
		EditorApplication.isPlaying = true;*/
	}
}
#endif