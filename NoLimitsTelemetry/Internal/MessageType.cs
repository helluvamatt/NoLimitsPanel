using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Internal
{
	/// <summary>
	/// Message type
	/// </summary>
	internal enum MessageType : ushort
	{
		/// <summary>
		/// Message Type ID: 0
		/// Category: Client Request
		/// Description: Can be send by the client to keep connection alive. No other purpose. Returned message by server is OK Message
		/// DataSize: 0
		/// </summary>
		Idle = 0,

		/// <summary>
		/// Message Type ID: 1
		/// Category: Server Reply
		/// Description: Typical answer from server for messages that were successfully processed and do not require a specific returned answer
		/// DataSize: 0
		/// </summary>
		Ok = 1,

		/// <summary>
		/// Message Type ID: 2
		/// Category: Server Reply
		/// Description: Will be send by the server in case of an error. The data component contains an UTF-8 encoded error message
		/// DataSize: Number of bytes of UTF8 encoded string
		/// </summary>
		Error = 2,

		/// <summary>
		/// Message Type ID: 3
		/// Category: Client Request
		/// Description: Can be used by the client to request the application version. The server will reply with Version Message
		/// DataSize: 0
		/// </summary>
		GetVersion = 3,

		/// <summary>
		/// Message Type ID: 4
		/// Category: Server Reply
		/// Description: Will be send by the server as an answer to Get Version Message
		/// DataSize: 4
		/// </summary>
		Version = 4,

		/// <summary>
		/// Message Type ID: 5
		/// Category: Client Request
		/// Description: Can be used by the client to request common telemetry data. The server will reply with Telemetry Message
		/// DataSize: 0
		/// </summary>
		GetTelemetry = 5,

		/// <summary>
		/// Message Type ID: 6
		/// Category: Server Reply
		/// Description: Will be send by the server as an anwser to Get Telemetry
		/// DataSize: 76
		/// </summary>
		Telemetry = 6,

		/// <summary>
		/// Message Type ID: 7
		/// Category: Client Request
		/// Description: Can be used by the client to request the number of coasters. The server will reply with Int Value Message
		/// DataSize: 0
		/// </summary>
		GetCoasterCount = 7,

		/// <summary>
		/// Message Type ID: 8
		/// Category: Server Reply
		/// Description: Will be send by the server as an answer to messages requesting various numbers
		/// DataSize: 4
		/// </summary>
		IntValue = 8,

		/// <summary>
		/// Message Type ID: 9
		/// Category: Client Request
		/// Description: Can be used by the client to request the name of a specific coaster. The server will reply with String Message
		/// DataSize: 4
		/// </summary>
		GetCoasterName = 9,

		/// <summary>
		/// Message Type ID: 10
		/// Category: Server Reply
		/// Description: Will be send by the server as an answer to messages requesting various strings
		/// DataSize: length of UTF8 encoded string
		/// </summary>
		String = 10,

		/// <summary>
		/// Message Type ID: 11
		/// Category: Client Request
		/// Description: Can be used by the client to request the current coaster and nearest station indices. The server will reply with Int Value Pair Message. First value will be current coaster index, second value will be nearest station index.
		/// DataSize: 0
		/// </summary>
		GetCurrentCoasterAndNearestStation = 11,

		/// <summary>
		/// Message Type ID: 12
		/// Category: Server Reply
		/// Description: Will be send by the server as an answer to messages requesting various value pairs
		/// DataSize: 8
		/// </summary>
		IntValuePair = 12,

		/// <summary>
		/// Message Type ID: 13
		/// Category: Client Request
		/// Description: Can be used by the client to set the emergency stop. The server will reply with OK Message
		/// DataSize: 5
		/// </summary>
		SetEmergencyStop = 13,

		/// <summary>
		/// Message Type ID: 14
		/// Category: Client Request
		/// Description: Can be used by the client to request the state of a specific station. The server will reply with Station State Message
		/// DataSize: 8
		/// </summary>
		GetStationState = 14,

		/// <summary>
		/// Message Type ID: 15
		/// Category: Server Reply
		/// Description: Will be send by server as an answer to a Get Station State Message
		/// DataSize: 4
		/// </summary>
		StationState = 15,

		/// <summary>
		/// Message Type ID: 16
		/// Category: Client Request
		/// Description: Can be used by the client to switch between manual and automatic station mode
		/// DataSize: 9
		/// </summary>
		SetManualMode = 16,

		/// <summary>
		/// Message Type ID: 17
		/// Category: Client Request
		/// Description: Can be used by the client to dispatch a train in manual mode
		/// DataSize: 8
		/// </summary>
		Dispatch = 17,

		/// <summary>
		/// Message Type ID: 18
		/// Category: Client Request
		/// Description: Can be used by the client to change gates in manual mode
		/// DataSize: 9
		/// </summary>
		SetGates = 18,

		/// <summary>
		/// Message Type ID: 19
		/// Category: Client Request
		/// Description: Can be used by the client to change harness in manual mode
		/// DataSize: 9
		/// </summary>
		SetHarness = 19,

		/// <summary>
		/// Message Type ID: 20
		/// Category: Client Request
		/// Description: Can be used by the client to lower/raise platform in manual modet
		/// DataSize: 9
		/// </summary>
		SetPlatform = 20,

		/// <summary>
		/// Message Type ID: 21
		/// Category: Client Request
		/// Description: Can be used by the client to lock/unlock flyer car in manual modet
		/// DataSize: 9
		/// </summary>
		SetFlyerCar = 21,
	}
}
