using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OSCSendExample : MonoBehaviour {

	public string targetIP = "127.0.0.1";
	public int targetPort = 1234;
	public string messageAddress = "/isadora";

	UnityOSC.OSCClient client;

	void Start() {
		client = new UnityOSC.OSCClient (System.Net.IPAddress.Parse (targetIP), targetPort);
	}

	// Update is called once per frame
	void OnTriggerEnter ( Collider other ) {
		SendMessage ();
	}

	void SendMessage() {
		UnityOSC.OSCMessage msg = new UnityOSC.OSCMessage (messageAddress);
		msg.Append (Time.time);

		client.Send (msg);
		client.Flush ();
		Debug.Log ("sent");
	}

	void OnDestroy() {
		client.Close ();
	}
}
