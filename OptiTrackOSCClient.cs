using UnityEngine;
using System.Collections;
using UnityOSC;
using System.Collections.Generic;
using System.Xml;

namespace HKUECT {

	public delegate void OSCMessageEventHandler(OSCMessage m);

	#region data classes
	[System.Serializable]
	public class SkeletonObjectMap {
		public string name = "";
		public List<string> bones = new List<string>();
		public List<Object> prefabs = new List<Object>();
		public bool alwaysSpawn = true;
		public Object genericPrefab;
	}

	[System.Serializable]
	public class SkeletonDefinition {
		public string name;
		public int id;
		public List<Vector3> positions;
		public List<Quaternion> rotations;
		public List<Vector3> offsets;
		public List<string> names;
		public List<int> parentIds;

		public SkeletonDefinition() {
			positions = new List<Vector3>();
			rotations = new List<Quaternion>();
			offsets = new List<Vector3>();
			names = new List<string>();
			parentIds = new List<int>();
		}

		public void Add(string name, Vector3 pos, Quaternion rot, Vector3 offset, int parentId) {
			names.Add(name);
			positions.Add(pos);
			rotations.Add(rot);
			offsets.Add(offset);
			parentIds.Add(parentId);
		}

		public int Count {
			get {
				return positions.Count;
			}
		}
	}

	[System.Serializable]
	public class RigidbodyDefinition {
		public string name;
		public int id;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 velocity;
		public Vector3 angularVelocity;
		public bool isActive;
	}

	#endregion

	/// <summary>
	/// Receives OSC data from the NatNet2OSCBridge at http://github.com/hku-ect 
	/// </summary>
	/// <remarks>
	/// Place this on a GameObject at the position where your OptiTrack root needs to be.
	/// Designed for a single instance of this script to be present per scene.
	/// </remarks>
	public class OptiTrackOSCClient : MonoBehaviour {
		public static event OSCMessageEventHandler unhandledMessageEvent;

		private static OptiTrackOSCClient instance;

		#region interface

		public int oscReceivePort = 6200;
		public float scale = 1f;

		/// <summary>
		/// We've found the x-axis to often be reversed when using Motive.
		/// This corrects for that flip (both positions + orientations)
		/// </summary>
		public bool flipX = false;

		public static bool GetSkeleton(string name, out SkeletonDefinition def) {
			def = null;
			if (instance == null)
				return false;

			if (instance.skeletons.ContainsKey(name)) {
				def = instance.skeletons [name];
				return true;
			}

			return false;
		}

		public static bool GetRigidbody(string name, out RigidbodyDefinition def) {
			def = null;
			if (instance == null)
				return false;

			if (instance.rigidbodies.ContainsKey(name)) {
				def = instance.rigidbodies [name];
				return true;
			}

			return false;
		}

		public static Vector3 GetPosition() {
			if (instance != null)
				return instance.transform.position;
			return Vector3.zero;
		}

		#endregion

		#region private members

		OSCServer server;

		Dictionary<string, SkeletonDefinition> skeletons = new Dictionary<string, SkeletonDefinition>();
		Dictionary<string, RigidbodyDefinition> rigidbodies = new Dictionary<string, RigidbodyDefinition>();

		Transform mTransform;
		Vector3 mPosition;

		#endregion

		#region private methods

		void FixValues(ref Vector3 p, ref Vector3 vel, ref Quaternion rot) {
			if (flipX) {
				Vector3 euler = rot.eulerAngles;
				euler.y = -euler.y;
				euler.z = -euler.z;
				rot.eulerAngles = euler;

				p.x = -p.x;
				vel.x = -vel.x;
			}
		}

		// Use this for initialization
		void Start() {
			if (instance != null) {
				Debug.LogError("There are two OptiTrackOSCClients, remove one");
			}
			instance = this;

			mTransform = transform;
			server = new OSCServer(oscReceivePort);
			mTransform.localScale = Vector2.one * scale;

			//playerObject.name = playerObjectName;

			server.SleepMilliseconds = 1;
			server.Connect();
			server.PacketReceivedEvent += PacketReceived;
		}

		void OnApplicationQuit() {
			server.Close();
		}

		void Update() {
			//update root position
			mPosition = mTransform.position;
		}
		
		// Update is called once per frame
		void PacketReceived(OSCServer sender, OSCPacket Packet) {
			if (Packet.IsBundle()) {
				for (int i = 0; i < Packet.Data.Count; ++i) {
					try {
						HandleMessage((OSCMessage)Packet.Data [i]);
					} catch (System.Exception e) {
						Debug.LogError(e.Message);
						Debug.LogError(e.StackTrace);
					}
				}
			} else {
				try {
					HandleMessage((OSCMessage)Packet);
				} catch (System.Exception e) {
					Debug.LogError(e.Message);
					Debug.LogError(e.StackTrace);
				}
			}
		}

		void HandleMessage(OSCMessage Packet) {
			//Debug.Log(Packet.Address);
			switch (Packet.Address) {
				case "/rigidBody":
					HandleRigidbody(Packet.Data);
					break;
				case "/skeleton":
					HandleSkeleton(Packet.Data);
					break;
				default:
					if (unhandledMessageEvent != null) {
						unhandledMessageEvent(Packet);
					}
					break;
			}
		}

		void HandleRigidbody(List<object> data) {
			//id
			//name
			//POSITION
			//x
			//y
			//z
			//ROTATION
			//x
			//y
			//z
			//w
			int index = 0;

			int id = (int)data [index++];
			string name = (string)data [index++];

			//Debug.Log( "rb: "+name );

			Vector3 position;
			position.x = (float)data [index++];
			position.y = (float)data [index++];
			position.z = (float)data [index++];

			Quaternion orientation;
			orientation.x = (float)data [index++];
			orientation.y = (float)data [index++];
			orientation.z = (float)data [index++];
			orientation.w = (float)data [index++];

			Vector3 velocity;
			velocity.x = (float)data [index++];
			velocity.y = (float)data [index++];
			velocity.z = (float)data [index++];

			Vector3 angVel;
			angVel.x = (float)data [index++];
			angVel.y = (float)data [index++];
			angVel.z = (float)data [index++];

			bool isActive = ((int)data [index++]) == 1;

			position = position * scale;

			FixValues(ref position, ref velocity, ref orientation);

			RigidbodyDefinition def;
			if (rigidbodies.ContainsKey(name)) {
				def = rigidbodies [name];
				def.position = position + mPosition;
				def.rotation = orientation;
				def.velocity = velocity;
				def.angularVelocity = angVel;
				def.isActive = isActive;
			} else {
				def = new RigidbodyDefinition();
				def.position = position + mPosition;
				def.rotation = orientation;
				def.velocity = velocity;
				def.angularVelocity = angVel;
				def.id = id;
				def.name = name;
				def.isActive = isActive;
				rigidbodies.Add(name, def);
			}
		}

		void HandleSkeleton(List<object> data) {
			int index = 0;

			//id
			//name
			string name = (string)data [index++];
			int id = (int)data [index++];

			SkeletonDefinition def;
			bool isNew = false;
			//check if this skeleton is already registered
			if (skeletons.ContainsKey(name)) {
				def = skeletons [name];
			} else {
				def = new SkeletonDefinition();
				def.name = name;
				def.id = id;
				skeletons.Add(name, def);
				isNew = true;
			}


			int jointIndex = 0;
			Vector3 vel = Vector3.zero;
			while (index < data.Count) {
				//PER JOINT
				string jointName = (string)data [index++];
				Vector3 pos;
				pos.x = (float)data [index++];
				pos.y = (float)data [index++];
				pos.z = (float)data [index++];
				
				Quaternion rot;
				rot.x = (float)data [index++];
				rot.y = (float)data [index++];
				rot.z = (float)data [index++];
				rot.w = (float)data [index++];

				//for retargeting
				int parentId = (int)data [index++];
				Vector3 offset;
				offset.x = (float)data [index++];
				offset.y = (float)data [index++];
				offset.z = (float)data [index++];

				pos = pos * scale;

				FixValues(ref pos, ref vel, ref rot);

				if (isNew) {
					//add everything
					def.Add(jointName, pos + mPosition, rot, offset, parentId);
				} else {
					//these are the only values that change
					def.positions [jointIndex] = pos + mPosition;
					def.rotations [jointIndex] = rot;
				}

				jointIndex++;
			}
		}

		#endregion
	}
}