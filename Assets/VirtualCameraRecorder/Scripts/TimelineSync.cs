using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace VCR {
	public class TimelineSync : MonoBehaviour {

		public AnimationClip masterClip;

		PlayableDirector pd;
		bool running = false;

		IEnumerator Start() {
			yield return new WaitForSeconds(3f);

			pd = GetComponent<PlayableDirector>();

			if ( masterClip == null ) {
				pd.timeUpdateMode = DirectorUpdateMode.GameTime;
				enabled = false;	
			}
			else {
				pd.timeUpdateMode = DirectorUpdateMode.Manual;
			}

			running = true;
		}
		
		// Update is called once per frame
		void Update () {
			if (running) {
				pd.time += Time.deltaTime;
				if (pd.time > masterClip.length) {
					pd.time -= masterClip.length;
				}
				pd.Evaluate();
			}
		}
	}
}