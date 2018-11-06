using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.PostProcessing;

namespace VCR {
	[ExecuteInEditMode]
	public class AnimatedCameraProperties : MonoBehaviour {
		public float focalDistance;
		public float aperture;

		public PostProcessingProfile postfx;

		private float _focalDistance, _aperture;

		CinemachineVirtualCamera vcam;
		DepthOfFieldModel.Settings settings;

		void Awake() {
			if ( postfx == null ) {
				postfx = FindObjectOfType<PostProcessingBehaviour>().profile;
			}

			_focalDistance = postfx.depthOfField.settings.focusDistance;
			_aperture = postfx.depthOfField.settings.aperture;

			focalDistance = _focalDistance;
			aperture = _aperture;

			vcam = GetComponent<CinemachineVirtualCamera>();
			if ( postfx != null ) {
				settings = postfx.depthOfField.settings;
			}
		}

		// Update is called once per frame
		void Update () {
			if ( focalDistance != _focalDistance || aperture != _aperture ) {
				Apply();
			}
		}

		void Apply() {
			if ( vcam == null ) vcam = GetComponent<CinemachineVirtualCamera>();
			
			settings.aperture = aperture;
			settings.focusDistance = focalDistance;

			postfx.depthOfField.settings = settings;

			_focalDistance = focalDistance;
			_focalDistance = aperture;
		}
	}
}