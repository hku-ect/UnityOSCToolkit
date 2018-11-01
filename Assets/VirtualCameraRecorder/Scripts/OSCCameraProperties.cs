using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VCR {
	[CreateAssetMenu]
	public class OSCCameraProperties : ScriptableObject {
		public float fov;
		public float focalDistance;
		public float aperture;
	}
}