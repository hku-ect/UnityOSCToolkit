using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;
using System.Net;

namespace HKUECT {
	public static class GearData {
		public const int RECEIVE_PORT = 5150;
		public const int SEND_PORT = 6505;

		public static string playerObjectName = "";
		public static string handShakeIP = "";
	}

	/// <summary>
	/// Controls tracking of the rigidbody of a motion-captured GearVR (all of them, so not just the player)
	/// </summary>
	/// <remarks>
	/// Requires handshaking with the GearVR Handshaker on http://github.com/hku-ect
	/// When verticalWalking is enabled, also sends its virtual position back to the handshaker
	/// and expects updates of virtual positions from other GearVRs
	/// </remarks>
	public class OptiTrackOSCGearVR : OptitrackRigidbody {
		public bool verticalWalking = false;
		public bool canFall = false;
		public bool withAccelleration = false;

		/// <summary>
		/// Relative rigidbodies will follow along with this object's y-offset when verticalWalking is enabled.
		/// </summary>
		/// <remarks>
		/// This has primarily been used to attach virtual hands to the person wearing the GearVR's.
		/// </remarks>
		public List<OptitrackRigidbody> relativeBodies = new List<OptitrackRigidbody>();

		#region private members

		string handShakedName = "[should be set automatically]";
		string handshakeIP = "[should be set automatically]";

		OSCClient handshakeClient;
		IPAddress handshakeIPAddress;
		Rigidbody rBody;

		/// <summary>
		/// The y-position reported by optitrack when we're virtually moving along this axis.
		/// This allows us to know how high we're supposed to be off the virtual floor.
		/// </summary>
		float physicalY = 0;

		#endregion

		#region protected methods

		protected virtual void Awake() {
			//we do this in awake to pre-empt any setup by targeted Rigidbodies
			if ( verticalWalking ) {
				//register to post update event
				for( int i = 0; i < relativeBodies.Count; ++i ) {
					relativeBodies[i].onPostUpdate += HandleLinkedRigidbody;
				}
			}
		}

		protected override void Start() {
			handShakedName = GearData.playerObjectName;
			handshakeIP = GearData.handShakeIP;

			name = rigidbodyName = handShakedName;

			deactiveWhenMissing = true;
			deactivateWhenUntracked = (name != handShakedName);

			if (verticalWalking) {
				OptiTrackOSCClient.unhandledMessageEvent += HandleMessage;

				if (IPAddress.TryParse(handshakeIP, out handshakeIPAddress)) {
					handshakeClient = new OSCClient(handshakeIPAddress, GearData.RECEIVE_PORT);
				} else {
					Debug.LogError("Invalid Handshaker IP. Did you forget to go through the handshaking scene?");
				}
			}
		}

		protected override void ApplyTransformUpdate(Vector3 position, Quaternion rotation) {
			//our gear
			if (rigidbodyName == handShakedName) {
				if (verticalWalking) {

					if (rBody == null) {
						rBody = t.GetComponent<Rigidbody>();
						if (rBody == null) {
							rBody = t.gameObject.AddComponent<Rigidbody>();
							rBody.useGravity = false;
						}
					}

					//snap to near-ground
					float yOffset = OptiTrackOSCClient.GetPosition().y;
					physicalY = position.y - yOffset;

					Vector3 objectPosition = t.position;

					//move object to correct X/Z position
					objectPosition.x = position.x;
					objectPosition.z = position.z;

					//raycast down from object
					RaycastHit hitInfo;
					//TODO: figure out if we need to raycast from closer to the floor (so we don't teleport on top of stuff too much)
					if (Physics.Raycast(objectPosition, -Vector3.up, out hitInfo, 2f, 1 << 8)) {
						objectPosition.y += (physicalY - hitInfo.distance);
						t.position = objectPosition;
						rBody.velocity = Vector3.zero;
					} else {
						t.position = objectPosition;
						if (canFall) {
							if (withAccelleration) {
								rBody.AddForce(-Vector3.up * 9.81f, ForceMode.Acceleration);
							} else {
								rBody.velocity = -3 * Vector3.up;
							}
						}
					}

					//send OSC Message for position
					OSCMessage m = new OSCMessage("/gear-virtualpos");
					m.Append(rigidbodyName);
					m.Append(objectPosition.x);
					m.Append(objectPosition.y);
					m.Append(objectPosition.z);
					handshakeClient.Send(m);
					handshakeClient.Flush();

					//when previewing in editor, apply rotation (Gear's handle this themselves)
					#if UNITY_EDITOR
					t.rotation = rotation;
					#endif
				} else {
					//TODO: test
					t.position = position;
					#if UNITY_EDITOR
					t.rotation = rotation;
					#endif
				}
			}
			//other player's gear
			else {
				if (verticalWalking) {
					//TODO: test
					//ignore y, but store it for reference
					float yOffset = OptiTrackOSCClient.GetPosition().y;
					physicalY = position.y - yOffset;

					Vector3 alternateP = t.position;
					alternateP.x = position.x;
					alternateP.z = position.z;
					t.position = alternateP;
				} else {
					//TODO: test
					//business as usual
					t.position = position;
					t.rotation = rotation;
				}
			}
		}

		#endregion

		#region private methods

		void HandleMessage(OSCMessage msg) {
			switch (msg.Address) {
				case "/gear-virtualpos":
					//do something!
					HandleVirtualGear(msg.Data);
					break;
			}
		}

		void HandleVirtualGear(List<object> data) {
			int index = 0;
			string incomingName = (string)data [index++];

			if (rigidbodyName == incomingName) {
				float x = (float)data [index++];
				float y = (float)data [index++];
				float z = (float)data [index++];

				Vector3 p;
				p.x = x;
				p.y = y;
				p.z = z;

				t.position = p;
			}
		}

		void HandleLinkedRigidbody( OptitrackRigidbody rb ) {
			Vector3 p = rb.transform.position;

			float yOffset = OptiTrackOSCClient.GetPosition().y;
			float diff = physicalY - ( p.y - yOffset );
			p.y += t.position.y + diff;

			rb.transform.position = p;
		}

		#endregion
	}
}