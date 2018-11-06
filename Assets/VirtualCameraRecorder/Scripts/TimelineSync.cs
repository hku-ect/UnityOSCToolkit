using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace VCR {
	public class TimelineSync : MonoBehaviour {

		static TimelineSync instance;

		public static bool Running {
			get {
				if (instance == null) return false;
				return instance.paused;
			}
		}

		public static void Pause() {
			if ( instance ) {
				instance.paused = true;
			}
		}

		public static void Resume() {
			if (instance) {
				instance.paused = false;
			}
		}

		public AnimationClip masterClip;

		PlayableDirector pd;
		bool running = false;
		bool paused = false;

		void Awake() {
			instance = this;
		}

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
				if ( !paused ) pd.time += Time.deltaTime;

				if (pd.time > masterClip.length) {
					pd.time -= masterClip.length;
				}
				pd.Evaluate();
			}
		}
	}
}