using UnityEngine;
using System.Collections;

namespace HKUECT {
	/// <summary>
	/// Tracks a named Rigidbody, but can ignore axes of position/rotation through settings.
	/// </summary>
	public class OptitrackSelectiveRigidbody : OptitrackRigidbody {
		#region settings

		public bool trackPosition = true;
		[Tooltip("Scales the position received from Mocap")]
		public Vector3 positionAxes = Vector3.one;
		public bool trackRotation = true;
		[Tooltip("Only whole values make sense here")] 
		public Vector3 rotationAxes = Vector3.one;
		public bool inverseRotation = false;
		[Tooltip("Relative to starting point in VIRTUAL space (prefab must be an existing scene object!)")]
		public bool relativeToStart = false;

		#endregion

		Vector3 startPos;

		#region protected methods

		protected override void Start() {
			base.Start();
			if (relativeToStart && prefab is GameObject) {
				startPos = (prefab as GameObject).transform.position;
			}
		}

		protected override void ApplyTransformUpdate(Vector3 position, Quaternion rotation) {
			if (trackRotation) {
				Vector3 euler = rotation.eulerAngles;
				if (inverseRotation)
					euler = Quaternion.Inverse(rotation).eulerAngles;

				euler.x *= rotationAxes.x;
				euler.y *= rotationAxes.y;
				euler.z *= rotationAxes.z;

				t.rotation = Quaternion.Euler(euler);
			}
			if (trackPosition) {
				Vector3 pos = position;

				pos.x *= positionAxes.x;
				pos.y *= positionAxes.y;
				pos.z *= positionAxes.z;

				if (relativeToStart) {
					pos += startPos;
				}

				t.position = pos;
			}
		}

		#endregion
	}
}