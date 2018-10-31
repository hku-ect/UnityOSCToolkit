using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Cinemachine;
using Cinemachine.Timeline;

namespace VCR {
	public static class Menus {

		static string cameraPrefab = "Assets/VirtualCameraRecorder/Prefabs/Main Camera.prefab";
		static string vcamPrefab = "Assets/VirtualCameraRecorder/Prefabs/VirtualCamera.prefab"; 

		[MenuItem("VCR/Setup")]
		static void Setup() {
			//check if we already have a timeline
			Object prefab = PrefabUtility.GetCorrespondingObjectFromSource(Camera.main);
			if ( prefab != null ) {
				string path = AssetDatabase.GetAssetPath(prefab);
				if ( path == cameraPrefab ) {
					return;
				}
			}
			SpawnSetup();
		}

		static void SpawnSetup() {
			if ( !EditorSceneManager.SaveOpenScenes() ) {
				Debug.LogError("Please save scene before running setup");
				return;
			}

			if ( Camera.main != null ) GameObject.DestroyImmediate(Camera.main.gameObject);

			Object cam = AssetDatabase.LoadAssetAtPath(cameraPrefab, typeof(Object));
			GameObject instantiatedCamera = (GameObject)PrefabUtility.InstantiatePrefab(cam);

			Object vcam = AssetDatabase.LoadAssetAtPath(vcamPrefab, typeof(Object));
			GameObject instantiatedVirtualCamera = (GameObject)PrefabUtility.InstantiatePrefab(vcam);			

			GameObject timeline = GameObject.Find("Timeline");
			if ( timeline == null ) {
				timeline = new GameObject("Timeline");
				PlayableDirector pd = timeline.AddComponent<PlayableDirector>();
				TimelineAsset tAsset = new TimelineAsset();
				pd.playableAsset = tAsset;

				AssetDatabase.CreateAsset( tAsset, "Assets/"+EditorSceneManager.GetActiveScene().name + ".playable");

				CinemachineTrack cmTrack = (CinemachineTrack)tAsset.CreateTrack(typeof(CinemachineTrack), null, "Cinemachine Track");
				pd.SetGenericBinding(cmTrack, instantiatedCamera.GetComponent<CinemachineBrain>());

				AnimationTrack animTrack = (AnimationTrack)tAsset.CreateTrack(typeof(AnimationTrack), null, "Virtual Camera");
				pd.SetGenericBinding(animTrack, instantiatedVirtualCamera.GetComponent<Animator>());
			}
		}

		[MenuItem("VCR/Start")]
		static void Start() {
			CameraShotRecorder.StartRecording();
		}

		[MenuItem("VCR/Stop")]
		static void Stop() {
			CameraShotRecorder.StopRecording();
		}
	}
}