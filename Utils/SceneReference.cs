using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace HKUECT {

	[System.Serializable]
	/// <summary>
/// Gives you the ability to drag scenes onto public variables.
/// </summary>
public class SceneReference {
		public string sceneName = "";
		public string id = null;
		public Object sceneObject = null;

		#region operators

		public static implicit operator string(SceneReference scene) {
			return scene.sceneName;
		}

		public static implicit operator Object(SceneReference scene) {
			return scene.sceneObject;
		}

		#endregion

		#region editor-specific

		#if UNITY_EDITOR
		[PostProcessScene(0)]
		public static void UpdateReference() {
			if (BuildPipeline.isBuildingPlayer) {
				MonoBehaviour[] scripts = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
				foreach (MonoBehaviour mb in scripts) {
					FieldInfo[] info = mb.GetType().GetFields();
					for (int i = 0; i < info.Length; ++i) {
						if (info [i].FieldType == typeof(SceneReference)) {
							SceneReference val = (SceneReference)info [i].GetValue(mb);
							if (val.sceneObject != null) {
								string path = AssetDatabase.GUIDToAssetPath(val.id);
								if (!string.IsNullOrEmpty(path)) {
									//Debug.Log ( "old value: "+val.sceneName );
									val.sceneName = GetSceneNameFromPath(path);
									//Debug.Log ( "new value: "+val.sceneName );
									info [i].SetValue(mb, val);
								} else {
									path = AssetDatabase.GetAssetPath(val.sceneObject);
									val.id = AssetDatabase.AssetPathToGUID(path);
									val.sceneName = GetSceneNameFromPath(path);
								}
							}
						}
					}
				}
			}
		}

		public void UpdateInstanceReference() {
			if (sceneObject != null) {
				string path = AssetDatabase.GUIDToAssetPath(id);
				if (!string.IsNullOrEmpty(path)) {
					sceneName = GetSceneNameFromPath(path);
				} else {
					path = AssetDatabase.GetAssetPath(sceneObject);
					id = AssetDatabase.AssetPathToGUID(path);
					sceneName = GetSceneNameFromPath(path);
				}
			} else if (!string.IsNullOrEmpty(sceneName)) {
				//try to find a scenefile with this name, and generate the other data
				string[] assets = AssetDatabase.FindAssets(sceneName);
				if (assets != null && assets.Length > 0) {
					string asset = null;

					foreach (string s in assets) {
						if (AssetDatabase.GUIDToAssetPath(s).EndsWith("/" + sceneName + ".unity")) {
							asset = s;
							break;
						}
					}

					if (asset != null) {
						id = asset;
						string path = AssetDatabase.GUIDToAssetPath(id);
						sceneObject = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
					} else {
						Debug.LogError("No scene asset found with name: " + sceneName + " (" + id + " / " + EditorSceneManager.GetActiveScene().name + ")");
					}
				} else {
					Debug.LogError("No scene asset found with name: " + sceneName + " (" + id + " / " + EditorSceneManager.GetActiveScene().name + ")");
				}
			}
		}

		public static string GetSceneNameFromPath(string path) {
			string[] splitPath = path.Split('/');
			string sceneFile = splitPath [splitPath.Length - 1];
			sceneFile = sceneFile.Substring(0, sceneFile.Length - 6);
			return sceneFile;
		}
		#endif

		#endregion
	}
}