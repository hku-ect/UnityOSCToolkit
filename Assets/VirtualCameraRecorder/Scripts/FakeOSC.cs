using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;
using System.Net;

namespace HKUECT {
	public class FakeOSC : MonoBehaviour {

		static OSCClient client;

		static OSCBundle bundle;

		bool clientOwner = false;

		// Use this for initialization
		void Start () {
			if (client == null) {
				client = new OSCClient (IPAddress.Parse ("127.0.0.1"), 6200);
				clientOwner = true;
			}
		}

		void OnDestroy() {
			if (client != null) {
				client.Close ();
				client = null;
			}
		}
		
		// Update is called once per frame
		void Update () {
			if ( bundle == null ) {
				bundle = new OSCBundle();
			}

			OSCMessage m = new OSCMessage ("/rigidBody");
			//id
			m.Append (0);
			//name
			m.Append (gameObject.name);
			//POSITION
			//x
			//y
			//z
			m.Append (-transform.position.x); //flipped x to match optitrack
			m.Append (transform.position.y);
			m.Append (transform.position.z);
			//ROTATION
			//x
			//y
			//z
			//w
			Quaternion rot = transform.rotation;
			Vector3 euler = rot.eulerAngles;
			euler.y = -euler.y;		//again flipped X to match Optitrack
			euler.z = -euler.z;
			rot.eulerAngles = euler;
			m.Append (rot.x);
			m.Append (rot.y);
			m.Append (rot.z);
			m.Append (rot.w);
			//VELOCITY
			//x
			//y
			//z
			m.Append (0f);
			m.Append (0f);
			m.Append (0f);
			//ANGULAR VELOCITY
			//x
			//y
			//z
			m.Append (0f);
			m.Append (0f);
			m.Append (0f);
			//ISACTIVE
			//bool (int)
			m.Append (1);

			//client.Send (m);
			//client.Flush ();
			bundle.Append(m);
		}

		void LateUpdate() {
			if ( clientOwner && client != null && bundle != null ) {
				client.Send(bundle);
				client.Flush();
				bundle = null;
			}
		}
	}
}