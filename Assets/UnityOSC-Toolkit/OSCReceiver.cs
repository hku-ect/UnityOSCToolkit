using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

namespace HKUECT
{
    public class OSCReceiver : MonoBehaviour
    {
        public string address = "/BareConductive";
        public int numValues = 13;
        public int listenPort = 6000;

        object[] data;
        OSCServer server;

        public object GetValue( int index ) {
            return data[index];
        }

        // Use this for initialization
        void Awake()
        {
            data = new object[numValues];

            server = new OSCServer(listenPort);
            server.SleepMilliseconds = 1;
            server.Connect();

            server.PacketReceivedEvent += PacketReceived;
        }

        void PacketReceived(OSCServer sender, OSCPacket Packet)
        {
            if (Packet.IsBundle())
            {
                for (int i = 0; i < Packet.Data.Count; ++i)
                {
                    try
                    {
                        HandleMessage((OSCMessage)Packet.Data[i]);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError(e.Message);
                        Debug.LogError(e.StackTrace);
                    }
                }
            }
            else
            {
                try
                {
                    HandleMessage((OSCMessage)Packet);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                    Debug.LogError(e.StackTrace);
                }
            }
        }

        void HandleMessage( OSCMessage msg ) {
            if ( msg.Address == address ) {
                int index = 0;
                while ( index < numValues ) {
                    data[index] = msg.Data[index++];
                }
            }
        }

        void OnDestroy() {
            server.Close();
        }
    }
}