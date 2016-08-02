using NoLimitsTelemetry.Data;
using NoLimitsTelemetry.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NoLimitsTelemetry
{
	public class NoLimitsTelemetryClient : IDisposable
	{
		#region Private members

		private Socket _Socket;
		private uint _RequestId;

		private bool _IsHeartbeatEnabled;
		private long _HeartbeatInterval;
		private Timer _HeartbeatTimer;
		private Timer _TelemetryTimer;
		private Timer _StationStateTimer;
		private int _StationStateCoasterId;
		private int _StationStateStationId;

		private ConcurrentDictionary<uint, RequestResponseContext> _ResponseContexts = new ConcurrentDictionary<uint, RequestResponseContext>();
		private BlockingCollection<Message> _Requests = new BlockingCollection<Message>();
		private Thread _SendThread;
		private Thread _ReceiveThread;
		private CancellationTokenSource _CancelTokenSource;

		private ErrorEventArgs _LastError = null;
		private object _LastErrorLockObj = new { };

		#endregion

		#region Properties

		/// <summary>
		/// Host name or IP address of telemetry server
		/// </summary>
		public string Host { get; private set; }

		/// <summary>
		/// Port number of telemetry server
		/// </summary>
		public int Port { get; private set; }

		/// <summary>
		/// Socket timeout in milliseconds
		/// </summary>
		public long SocketTimeout { get; set; }

		/// <summary>
		/// Should the client send a heartbeat (idle) message at regular intervals?
		/// </summary>
		public bool IsHeartbeatEnabled
		{
			get
			{
				return _IsHeartbeatEnabled;
			}
			set
			{
				_IsHeartbeatEnabled = value;
				long interval = _IsHeartbeatEnabled ? HeartbeatInterval : -1;
				_HeartbeatTimer.Change(interval, interval);
			}
		}

		/// <summary>
		/// Time in milliseconds between sending heartbeat (idle) messages
		/// </summary>
		public long HeartbeatInterval
		{
			get
			{
				return _HeartbeatInterval;
			}
			set
			{
				_HeartbeatInterval = value;
				if (IsHeartbeatEnabled)
					_HeartbeatTimer.Change(HeartbeatInterval, HeartbeatInterval);
			}
		}

		/// <summary>
		/// Time in milliseconds that the client should wait for server to respond to the heartbeat (idle) message before deciding the server is gone
		/// </summary>
		public long HeartbeatTimeout { get; set; }

		/// <summary>
		/// Determine if the client is connected to a telemetry server
		/// </summary>
		public bool Connected
		{
			get
			{
				return _Socket != null && _Socket.Connected;
			}
		}

		/// <summary>
		/// Retrieve the last error, could be null
		/// </summary>
		public ErrorEventArgs LastError
		{
			get
			{
				lock (_LastErrorLockObj)
				{
					return _LastError;
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Construct a telemetry client using the default port (15151)
		/// </summary>
		/// <param name="host">Hostname or IP address of telemetry server</param>
		public NoLimitsTelemetryClient(string host) : this(host, 15151) { }

		/// <summary>
		/// Construct a telemetry client using the specified host and port
		/// </summary>
		/// <param name="host">Hostname or IP address of telemetry server</param>
		/// <param name="port">TCP port of telemtry server</param>
		public NoLimitsTelemetryClient(string host, int port) : this(host, port, false) { }

		/// <summary>
		/// Construct a telemetry client using the specified host and port, also specify if the client should connect immediately
		/// </summary>
		/// <param name="host">Hostname or IP address of telemetry server</param>
		/// <param name="port">TCP port of telemetry server</param>
		/// <param name="connect">True to connect immediately</param>
		public NoLimitsTelemetryClient(string host, int port, bool connect)
		{
			// Set properties
			Host = host;
			Port = port;
			SocketTimeout = 30000;
			IsHeartbeatEnabled = true;
			HeartbeatInterval = 10000;
			HeartbeatTimeout = 5000;
			_RequestId = 0;

			// Build thread-safe socket for comms
			_Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			// Build timers for events
			_HeartbeatTimer = new Timer(_HeartbeatTimer_Callback);
			_TelemetryTimer = new Timer(_TelemetryTimer_Callback);
			_StationStateTimer = new Timer(_StationStateTimer_Callback);

			// Build worker threads
			_SendThread = new Thread(_SendThread_Loop);
			_ReceiveThread = new Thread(_ReceiveThread_Loop);
			_CancelTokenSource = new CancellationTokenSource();

			// Possibly connect now
			if (connect)
			{
				Connect();
			}
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Connect to Telemetry server
		/// </summary>
		public void Connect()
		{
			_Socket.Connect(Host, Port);
			_ReceiveThread.Start();
			_SendThread.Start();
		}

		/// <summary>
		/// Send an idle/noop message, used to keep the socket alive
		/// </summary>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		public bool Heartbeat()
		{
			return Heartbeat(SocketTimeout);
		}

		/// <summary>
		/// Send an idle/noop message, used to keep the socket alive
		/// </summary>
		/// <param name="timeout">Time in milliseconds that the client should wait for server to respond to the heartbeat (idle) message before deciding the server is gone</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		public bool Heartbeat(long timeout)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.Idle, requestId));
			Message response = WaitForResponse(requestId, timeout);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Get the server application version
		/// </summary>
		/// <returns>Server application version</returns>
		public Version GetVersion()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetVersion, requestId));
			Message response = WaitForResponse(requestId);
			return new Version(response.Data[0], response.Data[1], response.Data[2], response.Data[3]);
		}

		/// <summary>
		/// Get common telemetry data
		/// </summary>
		/// <returns>Telemetry</returns>
		public Telemetry GetTelemetry()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetTelemetry, requestId));
			Message response = WaitForResponse(requestId);
			return new Telemetry(response.Data);
		}

		/// <summary>
		/// Start requesting telemetry events
		/// </summary>
		/// <param name="interval">Approximate interval at which to request Telemetry data</param>
		public void StartTelemetry(TimeSpan interval)
		{
			_TelemetryTimer.Change(interval, interval);
		}

		/// <summary>
		/// Stop requesting telemetry events
		/// </summary>
		public void StopTelemetry()
		{
			_TelemetryTimer.Change(-1, -1);
		}

		/// <summary>
		/// Get the number of coasters
		/// </summary>
		/// <returns>Number of coasters (N)</returns>
		public int GetCoasterCount()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCoasterCount, requestId));
			Message response = WaitForResponse(requestId);
			return response.GetDataAsInt();
		}

		/// <summary>
		/// Get a coaster's name
		/// </summary>
		/// <param name="index">Index of coaster, 0..N-1</param>
		/// <returns>Name of the specified coaster</returns>
		/// <remarks>Use GetCoasterCount() to get the number of available coasters (N) </remarks>
		public string GetCoasterName(int index)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCoasterName, requestId).WithData(index));
			Message response = WaitForResponse(requestId);
			return response.GetDataAsString();
		}

		/// <summary>
		/// Get the current coaster and nearest station indices.
		/// </summary>
		/// <returns>Tuple, containing the requested information</returns>
		/// <remarks>First value will be current coaster index, second value will be nearest station index.</remarks>
		public Tuple<int, int> GetCurrentCoasterAndNearestStation()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCurrentCoasterAndNearestStation, requestId));
			Message response = WaitForResponse(requestId);
			return response.GetDataAsIntValuePair();
		}

		/// <summary>
		/// Set the emergency stop
		/// </summary>
		/// <param name="estop">Emergency stop state</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		public bool SetEmergencyStop(bool estop)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCurrentCoasterAndNearestStation, requestId));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Get the state of a specific station
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <returns>StationState flags</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public StationState GetStationState(int coasterIndex, int stationIndex)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetStationState, requestId).WithData(coasterIndex).WithData(stationIndex));
			Message response = WaitForResponse(requestId);
			return (StationState)response.GetDataAsInt();
		}

		/// <summary>
		/// Request Station State data on an interval
		/// </summary>
		/// <param name="interval">Approximate interval on which to send requests for station state</param>
		/// <param name="coasterIndex">Coaster index, see GetStationState</param>
		/// <param name="stationIndex">Station index, see GetStationState</param>
		public void StartStationState(TimeSpan interval, int coasterIndex, int stationIndex)
		{
			_StationStateCoasterId = coasterIndex;
			_StationStateStationId = stationIndex;
			_StationStateTimer.Change(interval, interval);
		}

		public void StopStationState()
		{
			_StationStateTimer.Change(-1, -1);
		}

		/// <summary>
		/// Switch between manual and automatic dispatch mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="manualMode">True for manual, false for automatic</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool SetManualMode(int coasterIndex, int stationIndex, bool manualMode)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetManualMode, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(manualMode));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Dispatch a train in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool Dispatch(int coasterIndex, int stationIndex)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.Dispatch, requestId).WithData(coasterIndex).WithData(stationIndex));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Change gates state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="open">True for open, false for closed</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool SetGates(int coasterIndex, int stationIndex, bool open)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetGates, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(open));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Change harness state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="open">True for open, false for closed</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool SetHarness(int coasterIndex, int stationIndex, bool open)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetHarness, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(open));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Change platform state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="lowered">True for lowered (ok to dispatch), false for raised (loading and unloading)</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool SetPlatform(int coasterIndex, int stationIndex, bool lowered)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetPlatform, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(lowered));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}

		/// <summary>
		/// Change flyer car lock state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="locked">True for locked, false for unlocked</param>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public bool SetFlyerCar(int coasterIndex, int stationIndex, bool locked)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetFlyerCar, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(locked));
			Message response = WaitForResponse(requestId);
			return response != null && response.MessageType == MessageType.Ok;
		}
		
		public void Dispose()
		{
			_CancelTokenSource.Cancel();
			_CancelTokenSource.Dispose();
			_CancelTokenSource = null;
			_Socket.Dispose();
			_Socket = null;
		}

		#endregion

		#region Events

		public event EventHandler<ErrorEventArgs> Error;

		public event EventHandler<TelemetryReceivedEventArgs> TelemetryReceived;

		public event EventHandler<StationStateReceivedEventArgs> StationStateReceived;

		#endregion

		#region Private methods

		private void AssertConnected()
		{
			if (_Socket == null) throw new InvalidOperationException("TcpClient was null");
			if (!_Socket.Connected) throw new InvalidOperationException("Client not connected!");
		}

		private uint GetNextRequestId()
		{
			return _RequestId++;
		}

		private void SendRequest(Message request)
		{
			_ResponseContexts.TryAdd(request.RequestID, new RequestResponseContext(request));
			_Requests.Add(request);
		}

		private Message WaitForResponse(uint requestId)
		{
			return WaitForResponse(requestId, SocketTimeout);
		}
		
		private Message WaitForResponse(uint requestId, long timeout)
		{
			RequestResponseContext ctxt;
			if (!_ResponseContexts.TryGetValue(requestId, out ctxt))
			{
				throw new InvalidOperationException("Invalid request ID: " + requestId);
			}
			ctxt.Wait(TimeSpan.FromMilliseconds(timeout));
			if (ctxt.Response == null)
			{
				OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, "Timed out waiting for response. Request = " + requestId, "TimeoutException", new TimeoutException().StackTrace));
			}
			return ctxt.Response;
		}

		private async Task<Message> WaitForResponseAsync(uint requestId)
		{
			return await Task.Run(() => WaitForResponse(requestId));
		}

		private void OnError(ErrorEventArgs args)
		{
			lock (_LastErrorLockObj)
			{
				_LastError = args;
			}
			Error?.Invoke(this, args);
		}

		private void _HeartbeatTimer_Callback(object state)
		{
			Heartbeat(HeartbeatTimeout);
		}

		private void _TelemetryTimer_Callback(object state)
		{
			Telemetry t = GetTelemetry();
			TelemetryReceived?.Invoke(this, new TelemetryReceivedEventArgs { TelemetryData = t });
		}

		private void _StationStateTimer_Callback(object state)
		{
			StationState s = GetStationState(_StationStateCoasterId, _StationStateStationId);
			StationStateReceived?.Invoke(this, new StationStateReceivedEventArgs(_StationStateCoasterId, _StationStateStationId, s));
		}

		private void _SendThread_Loop()
		{
			CancellationToken cancelToken = _CancelTokenSource.Token;
			while (!cancelToken.IsCancellationRequested)
			{
				try
				{
					Message message = _Requests.Take(cancelToken);
					_Socket.Send(message.GetMessagePayload());
				}
				catch (Exception ex)
				{
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}
			}
		}

		private void _ReceiveThread_Loop()
		{
			CancellationToken cancelToken = _CancelTokenSource.Token;
			List<byte> messageBuffer = new List<byte>();
			while (!cancelToken.IsCancellationRequested)
			{
				try
				{
					// Read until we have a message
					int start;
					int length;
					Message message;
					do
					{
						if (messageBuffer.Count > (ushort.MaxValue * 2))
						{
							throw new InvalidOperationException("Protocol error: server is sending data we don't understand. It is likely not a Telemetry server.");
						}

						byte[] buffer = new byte[1024];
						int rcvd = _Socket.Receive(buffer);
						messageBuffer.AddRange(buffer.Take(rcvd));
					}
					while (!Message.TryParseFromReceivedBytes(messageBuffer.ToArray(), out start, out length, out message));

					// Process the message
					RequestResponseContext ctxt;
					if (_ResponseContexts.TryGetValue(message.RequestID, out ctxt))
					{
						if (message.MessageType == MessageType.Error)
						{
							OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Server, "Server error: " + message.GetDataAsString(), null, null));
						}

						ctxt.ResponseArrived(message);
					}
					else
					{
						OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, "Server sent a response with an invalid RequestID: " + message.RequestID + "  MessageType: " + message.MessageType, null, null));
					}
				}
				catch (Exception ex)
				{
					messageBuffer.Clear();
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}

			}
		}

		#endregion
	}
}
