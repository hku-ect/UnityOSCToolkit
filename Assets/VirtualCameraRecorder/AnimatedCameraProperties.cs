using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.PostProcessing;

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

		//fov = Camera.main.fieldOfView;
		focalDistance = postfx.depthOfField.settings.focusDistance;
		aperture = postfx.depthOfField.settings.aperture;
		
		//_fov = fov;
		_focalDistance = focalDistance;
		_aperture = aperture;

		vcam = GetComponent<CinemachineVirtualCamera>();
		if ( postfx != null ) {
			settings = postfx.depthOfField.settings;
		}
	}

	// Update is called once per frame
	void Update () {
		if ( _focalDistance != focalDistance || _aperture != aperture ) {
			Apply();
		}
	}

	void Apply() {
		if ( vcam == null ) vcam = GetComponent<CinemachineVirtualCamera>();

		//vcam.m_Lens.FieldOfView = fov;
		settings.aperture = aperture;
		settings.focusDistance = focalDistance;

		postfx.depthOfField.settings = settings;

		//_fov = fov;
		_focalDistance = focalDistance;
		_aperture = aperture;
	}
}
