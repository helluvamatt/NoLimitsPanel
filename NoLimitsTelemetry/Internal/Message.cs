using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Internal
{
	internal sealed class Message
	{
		#region Constants

		/// <summary>
		/// Magic byte sent at the start of every message
		/// </summary>
		internal const byte MESSAGE_START_MAGIC = (byte)'N';
		
		/// <summary>
		/// Magic byte sent at the end of every message
		/// </summary>
		internal const byte MESSAGE_END_MAGIC = (byte)'L';

		#endregion

		#region Variables

		private List<byte> _Data;

		#endregion

		#region Constructor

		/// <summary>
		/// Create a message with empty data
		/// </summary>
		/// <param name="type">Message type</param>
		/// <param name="requestId">Request ID</param>
		/// <param name="data">Message data</param>
		public Message(MessageType type, uint requestId)
		{
			MessageType = type;
			RequestID = requestId;
			_Data = new List<byte>();
		}

		#endregion

		#region Properties

		/// <summary>
		/// Message type
		/// </summary>
		public MessageType MessageType { get; private set; }

		/// <summary>
		/// Request ID, used to track requests + responses
		/// </summary>
		public uint RequestID { get; private set; }

		/// <summary>
		/// Message data
		/// </summary>
		public byte[] Data
		{
			get
			{
				return _Data.ToArray();
			}
		}

		/// <summary>
		/// Length of message data
		/// </summary>
		public ushort Length
		{
			get
			{
				return (ushort)Data.Length;
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="bit">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(bool bit)
		{
			_Data.Add((byte)(bit ? 1 : 0));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(byte data)
		{
			_Data.Add(data);
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(uint data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(int data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(ushort data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(short data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(float data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current message, for chaining calls to this method</returns>
		public Message WithData(double data)
		{
			_Data.AddRange(Utils.GetBytes(data));
			return this;
		}

		/// <summary>
		/// Include the specified data
		/// </summary>
		/// <param name="data">Data to include</param>
		/// <returns>Reference to the current messgae, for chaining calls to this method</returns>
		public Message WithData(byte[] data)
		{
			_Data.AddRange(data);
			return this;
		}

		/// <summary>
		/// Generate the message data payload (including headers and footers)
		/// </summary>
		/// <returns>Array of bytes representing the entire message</returns>
		public byte[] GetMessagePayload()
		{
			// Create message payload and start with magic byte
			List<byte> payload = new List<byte>();
			payload.Add(MESSAGE_START_MAGIC);

			// Add the message type as big-endian (network byte order)
			byte[] messageType = Utils.GetBytes((ushort)MessageType);
			payload.AddRange(messageType);

			// Add Request ID
			byte[] requestId = Utils.GetBytes(RequestID);
			payload.AddRange(requestId);

			// Add DataSize
			byte[] dataSize = Utils.GetBytes(Length);
			payload.AddRange(dataSize);

			// Add Data
			payload.AddRange(Data);

			// Add message end magic byte
			payload.Add(MESSAGE_END_MAGIC);

			return payload.ToArray();
		}

		/// <summary>
		/// Get the Data as a UTF8 string
		/// </summary>
		/// <returns>String data</returns>
		/// <remarks>Only allowed on String and Error message types</remarks>
		public string GetDataAsString()
		{
			if (MessageType != MessageType.String && MessageType != MessageType.Error)
				throw new InvalidOperationException("Not a valid string message type.");
			return Encoding.UTF8.GetString(Data);
		}

		/// <summary>
		/// Get the Data as an int32
		/// </summary>
		/// <returns>Int32 data</returns>
		/// <remarks>Only allowed on IntValue message type</remarks>
		public int GetDataAsInt()
		{
			if (MessageType != MessageType.IntValue)
				throw new InvalidOperationException("Not an IntValue message type.");
			return BitConverter.ToInt32(CopyNativeBytes(), 0);
		}

		/// <summary>
		/// Get the Data as a IntValuePair
		/// </summary>
		/// <returns>Two ints as a Tuple</returns>
		/// <remarks>Only allowed on IntValuePair message type</remarks>
		public Tuple<int, int> GetDataAsIntValuePair()
		{
			if (MessageType != MessageType.IntValuePair)
				throw new InvalidOperationException("Not an IntValuePair message type.");
			int value1 = BitConverter.ToInt32(CopyNativeBytes(0, 4), 0);
			int value2 = BitConverter.ToInt32(CopyNativeBytes(4, 4), 0);
			return new Tuple<int, int>(value1, value2);
		}

		#endregion

		#region Private helpers

		private byte[] CopyData()
		{
			return CopyData(0, Data.Length);
		}

		private byte[] CopyData(int start, int length)
		{
			byte[] data = new byte[length];
			Array.Copy(Data, start, data, 0, length);
			return data;
		}

		private byte[] CopyNativeBytes()
		{
			return CopyNativeBytes(0, Data.Length);
		}

		private byte[] CopyNativeBytes(int start, int length)
		{
			byte[] data = CopyData(start, length);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(data);
			return data;
		}

		#endregion

		#region Public static methods

		/// <summary>
		/// Search for and parse a message from the given payload
		/// </summary>
		/// <param name="payload">Data payload received from the server</param>
		/// <param name="start">Start index of the slice containing the message</param>
		/// <param name="length">Length of the slice containing the message</param>
		/// <param name="message">Parsed message object</param>
		/// <returns>True if a message was parsed, false otherwise</returns>
		public static bool TryParseFromReceivedBytes(byte[] payload, out int start, out int length, out Message message)
		{
			// Initialize and search for the magic first byte
			length = 0;
			message = null;
			bool foundStart = false;
			for (start = 0; start < payload.Length; start++)
			{
				if (payload[start] == MESSAGE_START_MAGIC)
				{
					foundStart = true;
					break;
				}
			}
			if (!foundStart) return false;
			length++;

			// Parse message type
			byte[] messageTypePart = payload.Skip(start + length).Take(2).ToArray();
			if (messageTypePart.Length < 2) return false;
			ushort messageTypeId = Utils.GetUInt16(messageTypePart, 0);
			if (!Enum.IsDefined(typeof(MessageType), messageTypeId)) throw new InvalidOperationException("Invalid message type: " + messageTypeId);
			length += 2;

			// Parse request id
			byte[] requestIdPart = payload.Skip(start + length).Take(4).ToArray();
			if (requestIdPart.Length < 4) return false;
			uint requestId = Utils.GetUInt32(requestIdPart, 0);
			length += 4;

			// Parse message length
			byte[] dataLengthPart = payload.Skip(start + length).Take(2).ToArray();
			if (dataLengthPart.Length < 2) return false;
			ushort dataLength = Utils.GetUInt16(dataLengthPart, 0);
			length += 2;

			// Extract data
			byte[] dataPart = payload.Skip(start + length).Take(dataLength).ToArray();
			if (dataPart.Length != dataLength) return false;
			length += dataPart.Length;

			// Ensure footer exists
			if (payload[start + length] != MESSAGE_END_MAGIC) return false;
			length++;

			// Create and return the message object
			MessageType messageType = (MessageType)messageTypeId;
			message = new Message(messageType, requestId).WithData(dataPart);
			return true;
		}

		#endregion
	}
}