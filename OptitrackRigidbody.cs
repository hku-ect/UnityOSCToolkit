using UnityEngine;
using System.Collections;

namespace HKUECT {
	/// <summary>
	/// Tracks a Rigidbody found through the OptiTrackOSCClient.
	/// </summary>
	/// <remarks>
	/// Add this to an empty GameObject, and enter the desired name and prefab to be spawned for the Rigidbody.
	/// </remarks>
	public class OptitrackRigidbody : MonoBehaviour {
		public delegate void RigidbodyEvent( OptitrackRigidbody rb );

		const int MAX_FRAMES_UNTRACKED = 60;

		public string rigidbodyName = "";
		public Object prefab;
		public event RigidbodyEvent onPostUpdate;

		[Tooltip("If it is not sent by Motive")]
		public bool deactiveWhenMissing = true;
		[Tooltip("If it was not found (temporarily) by Motive")]
		public bool deactivateWhenUntracked = true;

		#region private & protected fields

		protected RigidbodyDefinition def;
		protected Transform t;

		bool active = true;
		int framesUntracked = 0;

		#endregion

		#region protected methods

		protected virtual void ApplyTransformUpdate(Vector3 position, Quaternion rotation) {
			t.position = def.position;
			t.rotation = def.rotation;

			if ( onPostUpdate != null ) onPostUpdate(this);
		}

		// Use this for initialization
		protected virtual void Start() {
			if (string.IsNullOrEmpty(rigidbodyName) || prefab == null) {
				enabled = false;
			}
		}
		
		// Update is called once per frame
		protected virtual void Update() {
			if (OptiTrackOSCClient.GetRigidbody(rigidbodyName, out def)) {
				if (def.isActive) {
					Activate();

					if (t == null) {
						GameObject g = GameObject.Find(prefab.name);
						if (g != null && g == prefab) {
							t = g.transform;
						} else {
							g = (Instantiate(prefab) as GameObject);//.transform;
							g.name = rigidbodyName;
							t = g.transform;
						}
					}

					ApplyTransformUpdate(def.position, def.rotation);
					framesUntracked = 0;
				} else if (deactivateWhenUntracked) {
					//prevent untracked objects from lingering in the scene
					if (++framesUntracked > MAX_FRAMES_UNTRACKED) {
						Deactivate();
					}
				}
			} else if (deactiveWhenMissing) {
				Deactivate();
			}
		}

		#endregion

		#region private methods

		void Activate() {
			if (active) {
				return;
			}
			active = true;
			if (t != null) {
				t.gameObject.SetActive(true);
			}
		}

		void Deactivate() {
			if (!active) {
				return;
			}
			active = false;
			if (t != null) {
				t.gameObject.SetActive(false);
			}
		}

		#endregion
	}
}