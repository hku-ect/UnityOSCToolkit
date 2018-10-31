using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HKUECT;
using UnityOSC;
using UnityEditor;

namespace HKUECT {
	///<summary>
	/// Can record incoming motion capture OSC data into a scriptable object, and allows for offline baking to animation clips
	///</summary>
	public class OSCRecorder : MonoBehaviour {
		public OSCMocapTake take;
		public bool replaceExisting = false;
		public OSCMapping prefabMapping;
		public bool previewSpawnMapping = false;

		//UnityEditor.SerializedObject serializedObject;
		//SerializedProperty startTicks, frameBundles, frameTimes;

		List<OSCBundle> bundles = new List<OSCBundle>();

		void Awake() {
			if ( previewSpawnMapping ) prefabMapping.Spawn(gameObject);
		}

		void Start() {
			if ( take.frameBundles.Count != 0 && replaceExisting == false ) {
				Debug.LogError("Please select replaceExisting to overwrite previously recorded data of an existing take");
				enabled = false;
				return;
			}

			OptiTrackOSCClient.onBundleReceived += RecordFrame;

			//serializedObject = new UnityEditor.SerializedObject(take);
			//startTicks = serializedObject.FindProperty("startTicks");
			//frameBundles = serializedObject.FindProperty("frameBundles");
			//frameTimes = serializedObject.FindProperty("frameTimes");

			take.startTicks = System.DateTime.Now.Ticks;
			take.frameBundles = new List<OSCBundleData>();
			take.frameTimes = new List<long>();
		}

		/*
		void OnApplicationQuit() {
			Debug.Log(bundles.Count);
			for( int i = 0; i < bundles.Count; ++i ) {
				Debug.Log(i);
				frameBundles.arraySize++;
				var newEntry = frameBundles.GetArrayElementAtIndex(frameBundles.arraySize - 1);
				SerializedProperty bundleAddress = newEntry.FindPropertyRelative("Address");
				SerializedProperty bundleData = newEntry.FindPropertyRelative("Data");
				bundleAddress.stringValue = bundles[i].Address;
				
				foreach( OSCMessage msg in bundles[i].Data ) {
					bundleData.arraySize++;
					var newMsg = bundleData.GetArrayElementAtIndex(bundleData.arraySize-1);
					SerializedProperty msgAddress = newMsg.FindPropertyRelative("Address");
					SerializedProperty msgData = newMsg.FindPropertyRelative("Data");
					SerializedProperty msgTypes = newMsg.FindPropertyRelative("Types");
					msgAddress.stringValue = msg.Address;

					foreach( object o in msg.Data ) {
						msgData.arraySize++;
						msgTypes.arraySize++;

						var newObj = msgData.GetArrayElementAtIndex(msgData.arraySize - 1);
						var newType = msgTypes.GetArrayElementAtIndex(msgTypes.arraySize - 1);

						newObj.stringValue = o.ToString();
						if ( o is int ) 		newType.stringValue = typeof(int).ToString();
						else if ( o is float )  newType.stringValue = typeof(float).ToString();
						else if ( o is string ) newType.stringValue = typeof(string).ToString();
						else Debug.LogError("Unhandled type for data: "+o.ToString());
						//TODO: Anythingf else?
					}
				}
			}

			Debug.Log("Saving?");
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}
		*/

		void RecordFrame( OSCBundle b ) {
			//bundles.Add(b);
			take.frameTimes.Add(System.DateTime.Now.Ticks);
			OSCBundleData bData = new OSCBundleData(b.Address, b.Data);
			take.frameBundles.Add(bData);
		}
	}
}