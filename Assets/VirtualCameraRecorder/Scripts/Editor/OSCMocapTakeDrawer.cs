using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityOSC;

namespace HKUECT {
	[CustomEditor(typeof(OSCMocapTake))]
	public class OSCMocapTakeInspector : Editor {
		const int RIGIDBODY_NAME_INDEX = 1;
		const int SKELETON_NAME_INDEX = 0;

		int frameCount = 0;
		float duration = 0;
		List<string> rigidbodies = new List<string>();
		List<string> skeletons = new List<string>();

		void OnEnable() {
			rigidbodies.Clear();
			skeletons.Clear();

			OSCMocapTake take = ( target as OSCMocapTake );
			frameCount = take.frameBundles.Count;
			
			foreach( OSCBundleData b in take.frameBundles ) {
				ParseBundle(b);
			}

			if ( take.frameTimes != null && take.frameTimes.Count >= 2 ) {
				long startTicks = take.frameTimes[0];
				long endTicks = take.frameTimes[take.frameTimes.Count - 1];

				System.TimeSpan s = System.TimeSpan.FromTicks(endTicks-startTicks);
				duration = (float)s.TotalSeconds;
			}

			EditorUtility.SetDirty(target);
		}

		void ParseBundle(OSCBundleData b) {
			if ( b == null ) return;
			foreach( OSCMessageData m in b.Data ) {
				ParseMessage(m);
			}
		}

		void ParseMessage( OSCMessageData m ) {
			switch( m.Address ) {
				case "/rigidBody":
					if ( !rigidbodies.Contains(m.Data[ RIGIDBODY_NAME_INDEX ].stringValue) ) {
						rigidbodies.Add(m.Data[ RIGIDBODY_NAME_INDEX ].stringValue);
					}
				break;
				case "/skeleton":
					if ( !skeletons.Contains(m.Data[ SKELETON_NAME_INDEX ].stringValue) ) {
						skeletons.Add(m.Data[ SKELETON_NAME_INDEX ].stringValue);
					}
				break;
			}
		}

		public override void OnInspectorGUI () {
			OSCMocapTake take = ( target as OSCMocapTake );

			//time stats
			EditorGUILayout.LabelField("Duration: "+ ( Mathf.Round( duration * 100 ) / 100.0f ) );

			//frame stats
			EditorGUILayout.LabelField( "Framecount: " + frameCount );

			//rigidbody statistics
			EditorGUILayout.LabelField( "Rigidbodies: " + rigidbodies.Count );
			EditorGUI.indentLevel++;
			foreach( string s in rigidbodies ) {
				EditorGUILayout.LabelField(s);
			}
			EditorGUI.indentLevel--;

			//skeleton statistics
			EditorGUILayout.LabelField( "Skeletons: " + skeletons.Count );
			EditorGUI.indentLevel++;
			foreach( string s in skeletons ) {
				EditorGUILayout.LabelField(s);
			}
			EditorGUI.indentLevel--;
		}
	}
}