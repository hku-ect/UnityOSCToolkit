using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HKUECT;
using UnityOSC;

namespace HKUECT {
	public class OSCPlayer : MonoBehaviour {

		public OSCMocapTake take;
		public string bakeName = "";
		public OSCMapping mapping;
		public int sendPort = 6200;
		OSCClient client;

		void Start() {
			if ( take != null && mapping != null ) {
				mapping.Spawn(gameObject);

				client = new OSCClient(System.Net.IPAddress.Parse("127.0.0.1"), sendPort);
				client.Connect();

				StartCoroutine(RunTake(take, mapping));
			}
			else {
				Debug.LogError("Take or Map null, please create valid assets and assign before playing.");
			}
		}

		IEnumerator RunTake( OSCMocapTake take, OSCMapping mapping ) {
			//send OSCBundles to localhost, by reconstructing them from the recorded take
			OSCBundle sendBundle;
			long lastTicks = take.startTicks;

			//parse bundles
			for( int i = 0; i < take.frameBundles.Count; ++i ) {
				sendBundle = new OSCBundle();

				OSCBundleData bData = take.frameBundles[i];
				sendBundle.Address = bData.Address;

				//parse messages of bundle
				for( int x = 0; x < bData.Data.Count; ++x ) {
					OSCMessageData msgData =  bData.Data[x];
					OSCMessage sendMsg = new OSCMessage(msgData.Address);
					
					//get correct data from msgData
					for( int y = 0; y < msgData.Data.Count; ++y ) {
						OSCDataInstance dataInst = msgData.Data[y];
						switch( dataInst.type ) {
							case OSCDataType.FLOAT:
								sendMsg.Append(dataInst.floatValue);
							break;
							case OSCDataType.INT:
								sendMsg.Append(dataInst.intValue);
							break;
							case OSCDataType.STRING:
								sendMsg.Append(dataInst.stringValue);
							break;
						}
					}

					//add msg to bundle for sending
					sendBundle.Append(sendMsg);
				}

				//wait as long as the delay between the frames
				double s = System.TimeSpan.FromTicks( take.frameTimes[i] - lastTicks ).TotalSeconds;
				lastTicks = take.frameTimes[i];

				//send directly as simulated received packet to the OSCClient\
				//if ( OptiTrackOSCClient.onBundleReceived != null ) {
				//	OptiTrackOSCClient.onBundleReceived(sendBundle);
				//}
				client.Send(sendBundle);
				client.Flush();

				yield return new WaitForSeconds( (float)s );
			}		

			yield return null;
		}
	}
}