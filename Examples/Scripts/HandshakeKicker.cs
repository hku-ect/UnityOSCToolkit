using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HKUECT;

[RequireComponent(typeof(GearVRHandshaker))]
public class HandshakeKicker : MonoBehaviour {
	// Use this for initialization
	void Start () {
        GetComponent<GearVRHandshaker>().Handshake();
	}
}
