//
//	  UnityOSC - Open Sound Control interface for the Unity3d game engine
//
//	  Copyright (c) 2012 Jorge Garcia Martin
//
// 	  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// 	  documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// 	  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// 	  and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// 	  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// 	  of the Software.
//
// 	  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// 	  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// 	  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// 	  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// 	  IN THE SOFTWARE.
//

using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

#if NETFX_CORE
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else
using System.Net.Sockets;
using System.Threading;
#endif



namespace UnityOSC
{
    public delegate void PacketReceivedEventHandler(OSCServer sender, OSCPacket packet);

	/// <summary>
	/// Receives incoming OSC messages
	/// </summary>
	public class OSCServer
    {
        #region Delegates
        public event PacketReceivedEventHandler PacketReceivedEvent;
        #endregion

        #region Constructors
        public OSCServer (int localPort)
		{
            PacketReceivedEvent += delegate(OSCServer s, OSCPacket p) { };

			_localPort = localPort;
			Connect();
		}
		#endregion
		
		#region Member Variables

#if NETFX_CORE
		DatagramSocket socket;
		private OSCPacket _lastReceivedPacket;
#else
		private UdpClient _udpClient;
		private Thread _receiverThread;
		private OSCPacket _lastReceivedPacket;
#endif
        private int _localPort;
        private int _sleepMilliseconds = 10;
		#endregion
		
		#region Properties
#if NETFX_CORE
		private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
		Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args)
		{
			// lock multi event 
			socket.MessageReceived -= Socket_MessageReceived;

            //Debug.Log("OSCSERVER UWP  Socket_MessageReceived");

            //Read the message that was received from the UDP echo client.
            //Stream streamIn = args.GetDataStream().AsStreamForRead();
            DataReader reader = args.GetDataReader();

            //StreamReader reader = new StreamReader(streamIn);
            try
            {

                uint stringLength = reader.UnconsumedBufferLength;
                byte[] bytes = new byte[stringLength];
                reader.ReadBytes(bytes);

                //string message = await reader.ReadToEndAsync()
                //                 .ConfigureAwait(continueOnCapturedContext: false);
                //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);

                OSCPacket packet = OSCPacket.Unpack(bytes);
                _lastReceivedPacket = packet;

                PacketReceivedEvent(this, _lastReceivedPacket); 
            }
            catch (System.Exception e)
            {
                WorldErrors.Print(e.Message);
            }
            finally
            {
                //streamIn.Dispose();
                reader.Dispose();
                // unlock multi event 
                socket.MessageReceived += Socket_MessageReceived;
            }
		}

#else
		public UdpClient UDPClient
		{
			get
			{
				return _udpClient;
			}
			set
			{
				_udpClient = value;
			}
		}
#endif
		
		public int LocalPort
		{
			get
			{
				return _localPort;
			}
			set
			{
				_localPort = value;
			}
		}
		
		public OSCPacket LastReceivedPacket
		{
			get
			{
				return _lastReceivedPacket;
			}
		}

		/// <summary>
		/// "Osc Receive Loop" sleep duration per message.
		/// </summary>
		/// <value>The sleep milliseconds.</value>
		public int SleepMilliseconds
		{
			get
			{
				return _sleepMilliseconds;
			}
			set
			{
				_sleepMilliseconds = value;
			}
		}
        #endregion

#region Methods

/// <summary>
/// Opens the server at the given port and starts the listener thread.
/// </summary>
/// 
#if NETFX_CORE
        public async void Connect()
        {
            Debug.Log("Waiting for a connection... " + _localPort.ToString());
            socket = new DatagramSocket();
			socket.Control.QualityOfService = SocketQualityOfService.LowLatency;
            socket.Control.DontFragment = true;
            socket.Control.InboundBufferSizeInBytes = 10000;
            socket.MessageReceived += Socket_MessageReceived;

            try
            {
                await socket.BindEndpointAsync(null, _localPort.ToString());

            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
                Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                return;
            }

        }
#else
        public void Connect()
		{
			if(this._udpClient != null) Close();
			try
			{
				_udpClient = new UdpClient(_localPort);
				_receiverThread = new Thread(new ThreadStart(this.ReceivePool));
				_receiverThread.Start();
			}
			catch(Exception e)
			{
				throw e;
			}
		}
#endif


        /// <summary>
        /// Closes the server and terminates its listener thread.
        /// </summary>
        public void Close()
		{
#if NETFX_CORE
			socket.Dispose();
#else
			if(_receiverThread !=null) _receiverThread.Abort();
			_receiverThread = null;
			_udpClient.Close();
			_udpClient = null;
#endif
		}

#if !NETFX_CORE
		/// <summary>
		/// Receives and unpacks an OSC packet.
        /// A <see cref="OSCPacket"/>
		/// </summary>
		private void Receive()
		{
			IPEndPoint ip = null;
			
			try
			{
				byte[] bytes = _udpClient.Receive(ref ip);

				if(bytes != null && bytes.Length > 0)
				{
                    OSCPacket packet = OSCPacket.Unpack(bytes);

                    _lastReceivedPacket = packet;

                    PacketReceivedEvent(this, _lastReceivedPacket);	
				}
			}
			catch (System.Exception e){
                //Don't throw errors on empty exceptions (caused by normal thread aborts)
                if (!string.IsNullOrEmpty(e.Message))
                {
                    throw new Exception(String.Format("Problem with server at port {0}. Error msg: {1}", _localPort, e.Message));
                }
  			}
		}
		
		/// <summary>
		/// Thread pool that receives upcoming messages.
		/// </summary>
		private void ReceivePool()
		{
			while( true )
			{
				Receive();
				
				Thread.Sleep(_sleepMilliseconds);
			}
		}
#endif
 #endregion
    }
}

