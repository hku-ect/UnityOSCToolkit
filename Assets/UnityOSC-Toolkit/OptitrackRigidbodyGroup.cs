using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HKUECT {
	[System.Serializable]
	public class NamePrefabMap {
		public string name = "";
		public Object prefab = null;
		[Tooltip("If it is not sent by Motive")]
		public bool deactivateWhenMissing = true;
		[Tooltip("If it was not found (temporarily) by Motive")]
		public bool deactivateWhenUntracked = true;
	}

	/// <summary>
	/// Allows for easily creating a bunch of Rigidbodies in one place.
	/// </summary>
	/// <remarks>
	/// Identical to making a lot of OptiTrackRigidbody's yourself, but much quicker.
	/// </remarks>
	public class OptitrackRigidbodyGroup : MonoBehaviour {
		public List<NamePrefabMap> objects = new List<NamePrefabMap>();

		void Start() {
			//Create objects
			for (int i = 0; i < objects.Count; ++i) {
				GameObject g = new GameObject("OSC-" + objects [i].name);
				OptitrackRigidbody rb = g.AddComponent<OptitrackRigidbody>();
				rb.rigidbodyName = objects [i].name;
				rb.prefab = objects [i].prefab;
				rb.deactiveWhenMissing = objects [i].deactivateWhenMissing;
				rb.deactivateWhenUntracked = objects [i].deactivateWhenUntracked;
				g.transform.parent = transform;
			}
		}
	}
}