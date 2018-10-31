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

using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnityOSC
{
	/// <summary>
	/// Models a Bundle of the OSC protocol.
	/// Derived from a OSC Packet over a OSC Stream.
	/// </summary>
	[System.Serializable]
	public sealed class OSCBundle : OSCPacket
	{
		#region Constructors
		public OSCBundle()
		{
			_address = BUNDLE;
		}
		
		public OSCBundle(long timestamp)
		{
			_address = BUNDLE;
			_timeStamp = timestamp;
		}
		#endregion
		
		#region Member Variables
		private const string BUNDLE = "#bundle";
		
		#endregion
		
		#region Properties
		#endregion
	
		#region Methods
		
		/// <summary>
		/// Specifies if the packet is an OSC bundle.
		/// </summary>
		override public bool IsBundle() { return true; }
		
		/// <summary>
		/// Packs a bundle to be transported over an OSC stream.
		/// </summary>
		override public void Pack()
		{
			// TODO: Pack bundle with timestamp in NTP format
			if ( _timeStamp == 0 ) {
				Decimal millis = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
				_timeStamp = BitConverter.ToInt64(ConvertToNtp(millis), 0);
			}

			List<byte> data = new List<byte>();

			data.AddRange(OSCPacket.PackValue(_address));
			OSCPacket.PadNull(data);

			data.AddRange(OSCPacket.PackValue(_timeStamp));
			OSCPacket.PadNull(data);

			foreach (OSCMessage msg in _data)
			{
				data.AddRange(msg.BinaryData);
			}

			this._binaryData = data.ToArray();
		}

		public static byte[] ConvertToNtp(decimal milliseconds)
		{
			decimal intpart = 0, fractpart = 0;
			var ntpData = new byte[8];

			intpart = milliseconds / 1000;
			fractpart = ((milliseconds % 1000) * 0x100000000L) / 1000m;

			Console.WriteLine("milliseconds: " + milliseconds);
			Console.WriteLine("intpart:      " + intpart);
			Console.WriteLine("fractpart:    " + fractpart);

			var temp = intpart;
			for (var i = 3; i >= 0; i--)
			{
				ntpData[i] = (byte)(temp % 256);
				temp = temp / 256;
			}

			temp = fractpart;
			for (var i = 7; i >= 4; i--)
			{
				ntpData[i] = (byte)(temp % 256);
				temp = temp / 256;
			}
			return ntpData;
		}

		/// <summary>
		/// Unpacks an OSC bundle from a data stream.
		/// </summary>
		/// <param name="data">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="start">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="end">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="OSCBundle"/>
		/// </returns>
		public new static OSCBundle Unpack(byte[] data, ref int start, int end)
		{
			string address = OSCPacket.UnpackValue<string>(data, ref start);
			
			#if !NETFX_CORE
			Trace.Assert(address == BUNDLE);
			#endif
			
			long timeStamp = OSCPacket.UnpackValue<long>(data, ref start);
			OSCBundle bundle = new OSCBundle(timeStamp);
			
			while(start < end)
			{
				int length = OSCPacket.UnpackValue<int>(data, ref start);
				int packetEnd = start + length;
				bundle.Append(OSCPacket.Unpack(data, ref start, packetEnd));
			}
			
			return bundle;
		}
		
		/// <summary>
		/// Appends an OSC message to a bundle.
		/// </summary>
		/// <param name="msgvalue">
		/// A <see cref="T"/>
		/// </param>
		public override void Append<T> (T msgvalue)
		{
			#if !NETFX_CORE
			Trace.Assert(msgvalue is OSCMessage);
			#endif
			_data.Add(msgvalue);
		}
		#endregion			
	}
}
