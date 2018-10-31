using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor.Experimental.Animations;
using UnityEngine.Animations;
using Cinemachine.Timeline;
using UnityEngine.PostProcessing;

namespace VCR {
	public class RuntimeVCam { 
		public string name;
		public Vector3 position;
		public Vector3 rotation;
		public CinemachineShot cmShot;

		public RuntimeVCam( string name, Vector3 rootPosition, Vector3 rootRotation, CinemachineShot cmShot ) {
			this.name = name;
			this.position = rootPosition;
			this.rotation = rootRotation;
			this.cmShot = cmShot;
		}
	}

	///<summary>
	/// Starts and Stops the recording of a runtime-animation of virtual cameras
	///</summary>
	[InitializeOnLoadAttribute]
	public static class CameraShotRecorder {
		public static bool Recording {
			get {
				return recording;
			}
		}
		static bool recording = false;

		static List<RuntimeVCam> vcams = new List<RuntimeVCam>();
		static DummyBehaviour helper;

		// register an event handler when the class is initialized
		static CameraShotRecorder()
		{
			EditorApplication.playModeStateChanged += LogPlayModeState;
			TouchOSCCamera.onStartRecording = StartRecording;
			TouchOSCCamera.onStopRecording = StopRecording;
		}

		private static void LogPlayModeState(PlayModeStateChange state)
		{
			if ( state == PlayModeStateChange.EnteredEditMode ) {
				CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
				if ( brain ) brain.enabled = true;

				GameObject TimelineGO = GameObject.Find("Timeline");
				if ( TimelineGO ) {
					PlayableDirector pd = TimelineGO.GetComponent<PlayableDirector>();

					foreach( RuntimeVCam vcam in vcams ) {
						GameObject vcamRoot = GameObject.FindWithTag("VirtualCamera");
						//vcamRoot.transform.position = vcam.position;
						//vcamRoot.transform.eulerAngles = vcam.rotation;
						//CinemachineVirtualCamera cam = vcamRoot.AddComponent<CinemachineVirtualCamera>();
						//Animator anim = vcamRoot.AddComponent<Animator>();
						//AnimatedCameraProperties camProps = vcamRoot.AddComponent<AnimatedCameraProperties>();
						//camProps.postfx = GameObject.FindObjectOfType<PostProcessingBehaviour>().profile;
						pd.playableGraph.GetResolver().SetReferenceValue(vcam.cmShot.VirtualCamera.exposedName, vcamRoot.GetComponent<CinemachineVirtualCamera>());
					}

					vcams.Clear();
				}
			}
			else if ( state == PlayModeStateChange.EnteredPlayMode ) {
				CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
				if ( brain ) brain.enabled = false;
			}
		}

		internal static void StartRecording() {
			if ( !Application.isPlaying ) return;

			if ( helper == null ) {
				GameObject g = new GameObject("_VCamRecorder");
				helper = g.AddComponent<DummyBehaviour>();
			}

			helper.StartCoroutine(DoRecording());
		}

		static IEnumerator DoRecording() {
			//TODO: Clean this shit up

			GameObject TimelineGO = GameObject.Find("Timeline");
			if ( TimelineGO == null ) {
				Debug.LogError("No Timeline! Please create an empty GameObject called 'Timeline', and create a Timeline animation on it from the Timeline Window");
				yield break;
			}

			CinemachineBrain mainBrain = Camera.main.GetComponent<CinemachineBrain>();
			if ( mainBrain == null ) {
				Debug.LogError("No CinemachineBrain on Main Camera. Make sure it exists!");
				yield break;
			}

			mainBrain.enabled = false;

			recording = true;
			TouchOSCCamera.CanApplySettings = false;

			string stamp = System.DateTime.Now.ToShortDateString() + "_" + System.DateTime.Now.ToLongTimeString();
			stamp = stamp.Replace("/", "-");
			stamp = stamp.Replace(":", "-");
			stamp = stamp.Replace(" ", "");

			//create virtual camera
			GameObject vcamRoot = GameObject.FindWithTag("VirtualCamera");//new GameObject(stamp);
			CinemachineVirtualCamera vcam = vcamRoot.GetComponent<CinemachineVirtualCamera>();
			//Animator anim = vcamRoot.AddComponent<Animator>();
			AnimatedCameraProperties camProps = vcamRoot.GetComponent<AnimatedCameraProperties>();
			//Debug.Log(GameObject.FindObjectOfType<PostProcessingBehaviour>().profile);
			//camProps.postfx = GameObject.FindObjectOfType<PostProcessingBehaviour>().profile;

			GameObjectRecorder recorder = new GameObjectRecorder(vcamRoot);
			recorder.BindAll(vcamRoot, false);
			
			PlayableDirector pd = TimelineGO.GetComponent<PlayableDirector>();
			TimelineAsset ta = pd.playableAsset as TimelineAsset;

			//This is to create an Animation Clip
			AnimationClip clip = new AnimationClip
			{
				name = stamp
			};
			UnityEditor.AnimationUtility.SetGenerateMotionCurves(clip, true);

			AssetDatabase.CreateAsset(clip, "Assets/VirtualCameraRecorder/Recordings/" + stamp + ".anim");
			AssetDatabase.SaveAssets();	
			
			if ( !pd.playableGraph.IsValid() ) pd.RebuildGraph();

			//instantiate playable for animationClip
			AnimationClipPlayable playable = AnimationClipPlayable.Create(pd.playableGraph, clip);

			//create new animationTrack on timeline
			//var newTrack = ta.CreateTrack<AnimationTrack>(null, vcamRoot.name);
			var rootTracks = ta.GetRootTracks();
			AnimationTrack newTrack = null;
			foreach( TrackAsset track in rootTracks ) {
				if ( track.name == "Virtual Camera") {
					newTrack = (AnimationTrack)track;
				}
			}

			//bind object to which the animation shall be assigned to the created animationTrack
			//pd.SetGenericBinding(newTrack, vcamRoot.GetComponent<Animator>());

			var timelineAnimClip = newTrack.CreateClip(clip);
			timelineAnimClip.displayName = stamp;

			( timelineAnimClip.asset as AnimationPlayableAsset ).position = Camera.main.transform.position;
			( timelineAnimClip.asset as AnimationPlayableAsset ).rotation = Camera.main.transform.rotation;

			//Get CinamechineTrack
			CinemachineTrack cinemachineTrack = (CinemachineTrack)ta.GetRootTrack(0);
			//create a timelineClip for the animationClip on the AnimationTrack
			TimelineClip cmTimelineClip = cinemachineTrack.CreateClip<CinemachineShot>();
			timelineAnimClip.start = pd.time;
			cmTimelineClip.start = pd.time;

			CinemachineShot shot = cmTimelineClip.asset as CinemachineShot;
			pd.playableGraph.GetResolver().SetReferenceValue(shot.VirtualCamera.exposedName, vcam);

			cmTimelineClip.displayName = stamp;//vcamRoot.name + " shot";
			
			vcams.Add(new RuntimeVCam( vcamRoot.name, Camera.main.transform.position, Camera.main.transform.eulerAngles, shot ) );

			DepthOfFieldModel.Settings settings = camProps.postfx.depthOfField.settings;

			Canvas recordCanvas = GameObject.FindGameObjectWithTag("RecordCanvas").GetComponent<Canvas>();
			recordCanvas.enabled = true;

			float fov, focalDistance, aperture;
			while ( recording ) {
				vcamRoot.transform.position = Camera.main.transform.position;

				TouchOSCCamera.GetData(out fov, out focalDistance, out aperture );
				
				Camera.main.fieldOfView = fov;
				vcam.m_Lens.FieldOfView = fov;

				camProps.focalDistance = focalDistance;
				camProps.aperture = aperture;

				settings.aperture = aperture;
				settings.focusDistance = focalDistance;
				camProps.postfx.depthOfField.settings = settings;

				recorder.TakeSnapshot(Time.deltaTime);

				if ( Time.frameCount % 30 == 0 ) recordCanvas.enabled = !recordCanvas.enabled;

				yield return null;
			}

			recordCanvas.enabled = false;

			//store recording as animation of virtual camera
			recorder.SaveToClip(clip);
			recorder.ResetRecording();

			cmTimelineClip.duration = timelineAnimClip.duration = clip.length;
			
			pd.RebuildGraph();
			mainBrain.enabled = true;

			TouchOSCCamera.CanApplySettings = true;
		}

		internal static void StopRecording() {
			recording = false;
		}
	}
}