using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HKUECT {
	public class OSCMapSpawner : MonoBehaviour {
		public OSCMapping map;
		public bool active = true;
		
		void Awake() {
			if ( active ) {
				map.Spawn(gameObject);
			}
		}
	}
}