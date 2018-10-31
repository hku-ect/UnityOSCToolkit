using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace VCR {
	public class TimelineSync : MonoBehaviour {

		public AnimationClip masterClip;

		PlayableDirector pd;

		void Start() {
			pd = GetComponent<PlayableDirector>();

			if ( masterClip == null ) {
				pd.timeUpdateMode = DirectorUpdateMode.GameTime;
				enabled = false;	
			}
			else {
				pd.timeUpdateMode = DirectorUpdateMode.Manual;
			}
		}
		
		// Update is called once per frame
		void Update () {
			pd.time += Time.deltaTime;
			if ( pd.time > masterClip.length ) {
				pd.time -= masterClip.length;
			}
			pd.Evaluate();
		}
	}
}