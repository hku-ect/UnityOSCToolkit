using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityOSC;
using System.Net;
using System.Collections.Generic;

namespace HKUECT {
	/// <summary>
	/// Handshake-script that communicates with the handshake ofxApp on http://github.com/hku-ect
	/// </summary>
	/// <remarks>
	/// Use this in combination with the OptiTrackOSCGearVR script when using GearVRs
	/// in combination with position tracking.
	/// </remarks>
	public class GearVRHandshaker : MonoBehaviour {
		public static string HANDSHAKE = "/gear-handshake";
		public static string HANDSHAKE_REPLY = "/gear-handshake-reply";
		public static string HANDSHAKE_BADIP = "/gear-handshake-badip";

		public delegate void Error(string error);

		public delegate void Success(string handShakedName);

		#region interface

		public event Error error;
		public event Success success;

		public string handshakeIP = "10.200.200.67";
		public bool autoLoadScene = false;
		public SceneReference targetScene;

		public void Handshake() {
			//setup server
			if (server == null) {
				server = new OSCServer(GearData.SEND_PORT);
				server.PacketReceivedEvent += (OSCServer sender, OSCPacket packet) => {
					foreach (OSCMessage m in packet.Data) {
						if (m.Address == HANDSHAKE_REPLY) {
							GearData.playerObjectName = (string)m.Data [0];
							GearData.handShakeIP = handshakeIP;
							if (success != null)
								success((string)m.Data [0]);

                            if (autoLoadScene && !string.IsNullOrEmpty(targetScene.sceneName)) {
                                doLoad = true;
                            }
                        } else if (m.Address == HANDSHAKE_BADIP) {
							DoError("Bad IP Reply");
							cancelRetry = true;
						} else {
							DoError("Unrecognized message: " + m.Address);
						}
					}
				};
			}
            
            DoHandshake();
		}

		#endregion

		#region private fields

		OSCClient client;
		OSCServer server;
		int port;
		float scale;
		IPAddress serverIP;
		bool cancelRetry = false;
        bool doLoad = false;

		#endregion

		#region private methods

        void Update() {
            if ( doLoad ) { 
                SceneManager.LoadScene(targetScene.sceneName);
                doLoad = false;
            }
        }

		void DoError(string err) {
			if (error != null) {
				error(err);
			}
		}

		void OnDestroy() {
			if (client != null)
				client.Close();
			if (server != null)
				server.Close();
		}

		void DoHandshake() {
			StopAllCoroutines();

			serverIP = IPAddress.Parse(handshakeIP);
			GearData.handShakeIP = handshakeIP;
			try {
				//send handshake to server
				if (client != null) {
					client.Close();
					client = new OSCClient(serverIP, GearData.RECEIVE_PORT);
				} else {
					client = new OSCClient(serverIP, GearData.RECEIVE_PORT); //handshake port is hardcoded
				}

				OSCMessage msg = new OSCMessage(HANDSHAKE, 0);
				client.Send(msg);
				
				StartCoroutine(RepeatHandshake(client, msg));
			} catch (System.Exception e) {
				cancelRetry = true;
				DoError("Could not connect to handshake server: " + e.StackTrace);
			}
		}

		IEnumerator RepeatHandshake(OSCClient client, OSCMessage msg) {
			while (true) {
				yield return new WaitForSeconds(1f);

				if (cancelRetry) {
					cancelRetry = false;
					client.Close();
					client = null;
					yield break;
				}

				DoError("No reply, retrying handshake");
				try {
					client.Send(msg);
				} catch (System.Exception e) {
					cancelRetry = true;
					client.Close();
					client = null;
					DoError("Could not send to handshake server: " + e.Message);
				}
			}
		}

		#endregion
	}
}