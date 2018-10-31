﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityOSC;
using Cinemachine;

namespace VCR {

	public delegate void Callback();

	public class TouchOSCCamera : MonoBehaviour {
		public static TouchOSCCamera instance {
			get;
			private set;
		}

		public static bool CanApplySettings {
			get;
			set;
		}
		
		public static void GetData( out float fov, out float focalDistance, out float aperture ) {
			if ( instance == null ) {
				fov = 65;
				focalDistance = 1;
				aperture = 1;
				return;
			}
			
			fov = instance.fov;
			focalDistance = instance.focalDistance;
			aperture = instance.aperture;
		}

		public static Callback onStartRecording;
		public static Callback onStopRecording;

		public PostProcessingProfile postfx;
		public int OSCPort = 6201;

		OSCServer server;

		DepthOfFieldModel.Settings settings;

		float fov;
		float focalDistance;
		float aperture;

		CinemachineBrain mainCameraBrain;

		bool recordEvent = false, stopEvent = false;

		void Awake() {
			CanApplySettings = true;
			instance = this;

			server = new OSCServer(OSCPort);
			server.SleepMilliseconds = 1;
			server.Connect();
			server.PacketReceivedEvent += PacketReceived;

			settings = postfx.depthOfField.settings;
		}

		void LateUpdate() {
			//main thread hacks
			if ( recordEvent ) {
				if ( onStartRecording != null ) onStartRecording();
				recordEvent = false;
			}
			if ( stopEvent ) {
				if ( onStopRecording != null ) onStopRecording();
				stopEvent = false;
			}

			if ( CanApplySettings ) ApplyProperties();
		}

		void OnDestroy() {
			server.Close();
		}

		void PacketReceived( OSCServer sender, OSCPacket packet ) {
			switch( packet.Address ) {
				case "/1/fov":
					fov = (float)packet.Data[0];
				break;
				case "/1/focalDistance":
					focalDistance = (float)packet.Data[0];
				break;
				case "/1/aperture":
					aperture = Mathf.Pow((float)packet.Data[0] / 12.0f, 4) * 12;
				break;
				case "/1/start":
					recordEvent = true;
				break;
				case "/1/stop":
					stopEvent = true;
				break;
			}
		}

		public void ApplyProperties() {
			Debug.Log("Applying Settings");

			if ( mainCameraBrain == null ) {
				mainCameraBrain = Camera.main.GetComponent<CinemachineBrain>();
			}

			if ( mainCameraBrain.ActiveVirtualCamera != null ) {
				CinemachineVirtualCamera vcam = mainCameraBrain.ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
				vcam.m_Lens.FieldOfView = fov;
			}
			
			Camera.main.fieldOfView = fov;

			settings.aperture = aperture;
			settings.focusDistance = focalDistance;

			postfx.depthOfField.settings = settings;
		}
	}
}