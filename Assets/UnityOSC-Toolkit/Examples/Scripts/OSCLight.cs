using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HKUECT;

[RequireComponent(typeof(Light))]
public class OSCLight : MonoBehaviour {

	public OSCReceiver receiver;

	Light _light;

	void Start() {
		_light = GetComponent<Light> ();
	}

	// Update is called once per frame
	void Update () {
		Color c = Color.white;
		c.r = (int)receiver.GetValue (0) / 127f;
		c.g = (int)receiver.GetValue (1) / 127f;
		c.b = (int)receiver.GetValue (2) / 127f;
		_light.color = c;
		_light.spotAngle = Mathf.Lerp( 10, 65, (int)receiver.GetValue (3) / 127f );
		_light.intensity = (int)receiver.GetValue (4) / 127f;
	}
}
