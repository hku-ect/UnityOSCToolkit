using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;
using HKUECT;

namespace HKUECT {
	[System.Serializable]
	public class RigidbodyMap {
		public string name = "";
		public Object prefab = null;
		public bool deactivateWhenMissing = true;
		public bool deactivateWhenUntracked = false;
	}

	[System.Serializable]
	public class SkeletonMap {
		public string name = "";
		public Avatar avatar;
		public Object prefab;
	}

	[System.Serializable]
	public enum OSCDataType {
		STRING = 0,
		INT = 1,
		FLOAT = 2
	}

	[System.Serializable]
	public class OSCDataInstance {
		public OSCDataType type;
		public int intValue;
		public float floatValue;
		public string stringValue;

		public OSCDataInstance ( OSCDataType type, object value ) {
			this.type = type;
			switch( type ) {
				case OSCDataType.INT:
					intValue = (int)value;
				break;
				case OSCDataType.FLOAT:
					floatValue = (float)value;
				break;
				case OSCDataType.STRING:
					stringValue = (string)value;
				break;
			}
		}
	}

	[System.Serializable]
	public class OSCMessageData {
		public string Address;
		public List<OSCDataInstance> Data = new List<OSCDataInstance>();

		public OSCMessageData( string address, List<object> data ) {
			this.Address = address;
			this.Data = new List<OSCDataInstance>();
			foreach( object o in data ) {
				OSCDataType t = OSCDataType.INT;
				if ( o is int )			t = OSCDataType.INT;
				else if ( o is float ) 	t = OSCDataType.FLOAT;
				else if ( o is string ) t = OSCDataType.STRING;
				else Debug.LogError("Unhandled type for data: "+o.ToString());
				//TODO: Anything else?
				this.Data.Add( new OSCDataInstance( t, o ) );
			}
		}
	}

	[System.Serializable]
	public class OSCBundleData {
		public string Address;
		public List<OSCMessageData> Data = new List<OSCMessageData>();

		public OSCBundleData( string address, List<object> data ) {
			this.Address = address;
			List<OSCMessageData> msgs = new List<OSCMessageData>();
			foreach( OSCMessage m in data ) {
				Data.Add(new OSCMessageData( m.Address, m.Data) );
			}
		}
	}

	[CreateAssetMenu]
	public class OSCMocapTake : ScriptableObject {
		public long startTicks = 0;
		public List<OSCBundleData> frameBundles = new List<OSCBundleData>();
		public List<long> frameTimes = new List<long>();

		void OnEnable() {
			hideFlags |= HideFlags.DontUnloadUnusedAsset;
		}
	}
}