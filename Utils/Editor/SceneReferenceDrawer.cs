using UnityEngine;
using System.Collections;
using UnityEditor;
using HKUECT;

[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferenceDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		SerializedProperty sceneObject = property.FindPropertyRelative("sceneObject");
		SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
		SerializedProperty sceneGUID = property.FindPropertyRelative("id");

		if (!string.IsNullOrEmpty(sceneGUID.stringValue) && sceneObject.objectReferenceValue == null) {
			//find the scene object
			sceneObject.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(sceneGUID.stringValue));
		}

		EditorGUI.BeginChangeCheck();
		EditorGUI.PropertyField(position, sceneObject, label);
		if (EditorGUI.EndChangeCheck()) {
			//check if it's actually a scene file!
			string path = AssetDatabase.GetAssetPath(sceneObject.objectReferenceInstanceIDValue);

			if (string.IsNullOrEmpty(path)) {
				sceneGUID.stringValue = string.Empty;
				sceneName.stringValue = string.Empty;
				sceneObject.objectReferenceValue = null;
			} else {
				if (path.Substring(path.Length - 6, 6) == ".unity") {
					sceneGUID.stringValue = AssetDatabase.AssetPathToGUID(path);
					sceneName.stringValue = SceneReference.GetSceneNameFromPath(path);
				} else {
					EditorUtility.DisplayDialog("Error", "Object is not a scene file", "OK");
					sceneObject.objectReferenceValue = null;
				}
			}
		}
	}
}