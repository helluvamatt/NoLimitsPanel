using NoLimitsTelemetry.Data;
using NoLimitsTelemetry.Events;
using NoLimitsTelemetry.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NoLimitsTelemetry
{
	public class NoLimitsTelemetryClient : IDisposable
	{
		#region Private members

		private Socket _Socket;
		private int _RequestId; // Casted to uint
		private bool _IsDisposed = false;

		private bool _IsHeartbeatEnabled;
		private long _HeartbeatInterval;
		private Timer _HeartbeatTimer;
		private Timer _TelemetryTimer;
		private Timer _StationStateTimer;
		private int _StationStateCoasterId;
		private int _StationStateStationId;
		private Timer _CurrentCoasterAndStationTimer;
		private int _NearestCoasterId = -1;
		private int _NearestStationId = -1;
		private bool _IsUpdatingNearest = false;
		private bool _NearestSet
		{
			get
			{
				return _NearestCoasterId > -1 && _NearestStationId > -1;
			}
		}

		private BlockingCollection<Message> _Requests = new BlockingCollection<Message>();
		private ConcurrentDictionary<uint, Message> _RequestsAwaitingResponses = new ConcurrentDictionary<uint, Message>();
		private HashSet<uint> _ExplicitRequests = new HashSet<uint>();
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
				return _HeartbeatTimer != null && _IsHeartbeatEnabled;
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

		/// <summary>
		/// When receiving automatic events for station state, should the coaster and station used follow the nearest coaster and station?
		/// </summary>
		public bool StationStateFollowsNearest { get; set; }

		/// <summary>
		/// Specify a simple trace message handler for debugging the client
		/// </summary>
		/// <remarks>
		/// This handler will receive many messages (dozens per operation)
		/// </remarks>
		public ITraceHandler TraceHandler { get; set; }

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
		public NoLimitsTelemetryClient(string host, int port)
		{
			// Set properties
			Host = host;
			Port = port;
			SocketTimeout = 30000;
			_IsHeartbeatEnabled = true;
			_HeartbeatInterval = 10000;
			HeartbeatTimeout = 5000;
			_RequestId = 0;

			// Build thread-safe socket for comms
			_Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			// Build timers for events
			_HeartbeatTimer = new Timer(_HeartbeatTimer_Callback);
			_TelemetryTimer = new Timer(_TelemetryTimer_Callback);
			_StationStateTimer = new Timer(_StationStateTimer_Callback);
			_CurrentCoasterAndStationTimer = new Timer(_CurrentCoasterAndStationTimer_Callback);

			// Build worker threads
			_SendThread = new Thread(_SendThread_Loop);
			_ReceiveThread = new Thread(_ReceiveThread_Loop);
			_CancelTokenSource = new CancellationTokenSource();
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Connect to Telemetry server
		/// </summary>
		public void Connect()
		{
			OnTrace("Entering...");
			_Socket.Connect(Host, Port);
			_ReceiveThread.Start();
			_SendThread.Start();
			OnTrace("Leaving...");
		}

		/// <summary>
		/// Send an idle/noop message, used to keep the socket alive
		/// </summary>
		/// <returns>True if OkMessage is returned, false otherwise (check errors, Error event handler may have been called as well)</returns>
		public uint Heartbeat()
		{
			return Heartbeat(SocketTimeout);
		}

		/// <summary>
		/// Send an idle/noop message, used to keep the socket alive
		/// </summary>
		/// <param name="timeout">Time in milliseconds that the client should wait for server to respond to the heartbeat (idle) message before deciding the server is gone</param>
		/// <returns>The request ID</returns>
		public uint Heartbeat(long timeout)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.Idle, requestId));
			return requestId;
		}

		/// <summary>
		/// Get the server application version
		/// </summary>
		/// <returns>Request ID</returns>
		public uint GetVersion()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetVersion, requestId));
			return requestId;
		}

		/// <summary>
		/// Get common telemetry data
		/// </summary>
		/// <returns>Telemetry</returns>
		public uint GetTelemetry()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetTelemetry, requestId));
			return requestId;
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
		/// <returns>Request ID</returns>
		public uint GetCoasterCount()
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCoasterCount, requestId));
			return requestId;
		}

		/// <summary>
		/// Get a coaster's name
		/// </summary>
		/// <param name="index">Index of coaster, 0..N-1</param>
		/// <returns>Name of the specified coaster</returns>
		/// <remarks>Use GetCoasterCount() to request the number of available coasters (N) </remarks>
		public uint GetCoasterName(int index)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCoasterName, requestId).WithData(index));
			return requestId;
		}

		/// <summary>
		/// Get the current coaster and nearest station indices.
		/// </summary>
		/// <returns>Request ID</returns>
		/// <remarks>First value will be current coaster index, second value will be nearest station index.</remarks>
		public uint GetCurrentCoasterAndNearestStation()
		{
			return GetCurrentCoasterAndNearestStation(true);
		}

		/// <summary>
		/// Request current coaster and nearest station on an interval
		/// </summary>
		/// <param name="interval">Approximate interval on which to send requests</param>
		public void StartCurrentCoasterAndNearestStation(TimeSpan interval)
		{
			_IsUpdatingNearest = true;
			_NearestCoasterId = -1;
			_NearestStationId = -1;
			_CurrentCoasterAndStationTimer.Change(interval, interval);
		}

		/// <summary>
		/// Stop requesting current coaster and nearest station
		/// </summary>
		public void StopCurrentCoasterAndNearestStation()
		{
			_IsUpdatingNearest = false;
			_CurrentCoasterAndStationTimer.Change(-1, -1);
			_NearestCoasterId = -1;
			_NearestStationId = -1;
		}

		/// <summary>
		/// Set the emergency stop
		/// </summary>
		/// <param name="estop">Emergency stop state</param>
		/// <returns>Request ID</returns>
		public uint SetEmergencyStop(bool estop)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCurrentCoasterAndNearestStation, requestId));
			return requestId;
		}

		/// <summary>
		/// Get the state of a specific station
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint GetStationState(int coasterIndex, int stationIndex)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetStationState, requestId).WithData(coasterIndex).WithData(stationIndex));
			return requestId;
		}

		/// <summary>
		/// Request Station State data on an interval
		/// </summary>
		/// <param name="interval">Approximate interval on which to send requests for station state</param>
		/// <param name="coasterIndex">Coaster index, see GetStationState</param>
		/// <param name="stationIndex">Station index, see GetStationState</param>
		/// <remarks>
		/// If <see cref="StationStateFollowsNearest"/> is set to True, the coasterIndex and stationIndex parameters here are ignored and the nearest coaster/station is used.
		/// </remarks>
		public void StartStationState(TimeSpan interval, int coasterIndex, int stationIndex)
		{
			_StationStateCoasterId = coasterIndex;
			_StationStateStationId = stationIndex;
			_StationStateTimer.Change(interval, interval);
		}

		/// <summary>
		/// Stop requesting station state data
		/// </summary>
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
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint SetManualMode(int coasterIndex, int stationIndex, bool manualMode)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetManualMode, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(manualMode));
			return requestId;
		}

		/// <summary>
		/// Dispatch a train in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint Dispatch(int coasterIndex, int stationIndex)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.Dispatch, requestId).WithData(coasterIndex).WithData(stationIndex));
			return requestId;
		}

		/// <summary>
		/// Change gates state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="open">True for open, false for closed</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint SetGates(int coasterIndex, int stationIndex, bool open)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetGates, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(open));
			return requestId;
		}

		/// <summary>
		/// Change harness state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="open">True for open, false for closed</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint SetHarness(int coasterIndex, int stationIndex, bool open)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetHarness, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(open));
			return requestId;
		}

		/// <summary>
		/// Change platform state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="lowered">True for lowered (ok to dispatch), false for raised (loading and unloading)</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint SetPlatform(int coasterIndex, int stationIndex, bool lowered)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetPlatform, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(lowered));
			return requestId;
		}

		/// <summary>
		/// Change flyer car lock state in manual mode
		/// </summary>
		/// <param name="coasterIndex">Coaster index</param>
		/// <param name="stationIndex">Station index</param>
		/// <param name="locked">True for locked, false for unlocked</param>
		/// <returns>Request ID</returns>
		/// <remarks>
		/// Use GetCoasterCount() to determine max coaster index.
		/// 
		/// Use GetNearestCoasterAndNearestStation() to determine coasterIndex and stationIndex
		/// </remarks>
		public uint SetFlyerCar(int coasterIndex, int stationIndex, bool locked)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.SetFlyerCar, requestId).WithData(coasterIndex).WithData(stationIndex).WithData(locked));
			return requestId;
		}
		
		/// <summary>
		/// Clean up this client, close connections, stop timers, etc...
		/// </summary>
		public void Dispose()
		{
			OnTrace("Entering...");
			StopCurrentCoasterAndNearestStation();
			StopStationState();
			StopTelemetry();
			_IsDisposed = true;
			_HeartbeatTimer.Dispose();
			_TelemetryTimer.Dispose();
			_StationStateTimer.Dispose();
			_CurrentCoasterAndStationTimer.Dispose();
			_CancelTokenSource.Cancel();
			_Socket.Shutdown(SocketShutdown.Both);
			_Socket.Close();
			_CancelTokenSource.Dispose();
			_Socket.Dispose();
			OnTrace("Leaving...");
		}

		#endregion

		#region Events

		public event EventHandler<ErrorEventArgs> Error;

		public event EventHandler<TelemetryReceivedEventArgs> TelemetryReceived;

		public event EventHandler<StationStateReceivedEventArgs> StationStateReceived;

		public event EventHandler<CurrentCoasterAndStationReceivedEventArgs> CurrentCoasterAndStationReceived;

		public event EventHandler<OkMessageReceivedEventArgs> OkMessageReceived;

		public event EventHandler<VersionReceivedEventArgs> VersionReceived;

		public event EventHandler<CoasterNameReceivedEventArgs> CoasterNameReceived;

		public event EventHandler<CoasterCountReceivedEventArgs> CoasterCountReceived;

		public event EventHandler<CurrentCoasterOrStationChangedEventArgs> CurrentCoasterOrStationChanged;

		#endregion

		#region Private methods

		#region Utilities

		private uint GetCurrentCoasterAndNearestStation(bool emitExplicitEvent)
		{
			AssertConnected();
			uint requestId = GetNextRequestId();
			SendRequest(new Message(MessageType.GetCurrentCoasterAndNearestStation, requestId));
			if (emitExplicitEvent) _ExplicitRequests.Add(requestId);
			return requestId;
		}

		private void AssertConnected()
		{
			if (_IsDisposed) throw new ObjectDisposedException("Client is disposed.");
			if (_Socket == null || !_Socket.Connected) throw new InvalidOperationException("Client is not connected.");
		}

		private uint GetNextRequestId()
		{
			return (uint) Interlocked.Increment(ref _RequestId);
		}

		private void SendRequest(Message request)
		{
			_Requests.Add(request);
		}

		private void ProcessResponse(Message response)
		{
			// Grab the associated request
			Message request;
			if (_RequestsAwaitingResponses.TryRemove(response.RequestID, out request))
			{
				// Process the message
				if (response.MessageType == MessageType.Error)
				{
					OnError(new ErrorEventArgs(response.RequestID, ErrorEventArgs.ErrorType.Server, "Server error: " + response.GetDataAsString(), null, null));
				}
				else if (response.MessageType == MessageType.Ok)
				{
					OkMessageReceived?.Invoke(this, new OkMessageReceivedEventArgs(request.RequestID));
				}
				else if (response.MessageType == MessageType.Version && request.MessageType == MessageType.GetVersion)
				{
					VersionReceived?.Invoke(this, new VersionReceivedEventArgs(request.RequestID, new Version(response.Data[0], response.Data[1], response.Data[2], response.Data[3])));
				}
				else if (response.MessageType == MessageType.Telemetry && request.MessageType == MessageType.GetTelemetry)
				{
					TelemetryReceived?.Invoke(this, new TelemetryReceivedEventArgs(request.RequestID, new Telemetry(response.Data)));
				}
				else if (response.MessageType == MessageType.StationState && request.MessageType == MessageType.GetStationState)
				{
					StationStateReceived?.Invoke(this, new StationStateReceivedEventArgs(request.RequestID, _StationStateCoasterId, _StationStateStationId, (StationState)response.GetDataAsInt()));
				}
				else if (response.MessageType == MessageType.IntValuePair && request.MessageType == MessageType.GetCurrentCoasterAndNearestStation)
				{
					var data = response.GetDataAsIntValuePair();
					var cur = new CurrentCoasterAndStation(data.Item1, data.Item2);
					if (_ExplicitRequests.Remove(request.RequestID))
					{
						CurrentCoasterAndStationReceived?.Invoke(this, new CurrentCoasterAndStationReceivedEventArgs(request.RequestID, cur));
					}
					if (_NearestCoasterId != cur.CurrentCoaster || _NearestStationId != cur.CurrentStation)
					{
						_NearestCoasterId = cur.CurrentCoaster;
						_NearestStationId = cur.CurrentStation;
						CurrentCoasterOrStationChanged?.Invoke(this, new CurrentCoasterOrStationChangedEventArgs(cur));
					}
				}
				else if (response.MessageType == MessageType.IntValue && request.MessageType == MessageType.GetCoasterCount)
				{
					CoasterCountReceived?.Invoke(this, new CoasterCountReceivedEventArgs(request.RequestID, response.GetDataAsInt()));
				}
				else if (response.MessageType == MessageType.String && request.MessageType == MessageType.GetCoasterName)
				{
					CoasterNameReceived?.Invoke(this, new CoasterNameReceivedEventArgs(request.RequestID, 0, response.GetDataAsString()));
				}
				else
				{
					string errorMsg = string.Format("Server sent an invalid response: RequestID = {0}  RequestType = {1}  ResponseType = {2}", request.RequestID, request.MessageType.ToString(), response.MessageType.ToString());
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, errorMsg, null, null));
				}
			}
			else
			{
				string errorMsg = string.Format("Server sent a response for an invalid request ID: {0}  Type: {1} ", response.RequestID, response.MessageType.ToString());
				OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, errorMsg, null, null));
			}
		}

		private void OnError(ErrorEventArgs args)
		{
			lock (_LastErrorLockObj)
			{
				_LastError = args;
			}
			Error?.Invoke(this, args);
		}

		private void OnTrace(string message, [CallerMemberName] string caller = "Unknown")
		{
			string threadName = Thread.CurrentThread.Name;
			if (string.IsNullOrEmpty(threadName)) threadName = string.Format("Thread {0}", Thread.CurrentThread.ManagedThreadId);
			if (TraceHandler != null) TraceHandler.TraceMsg(threadName, caller, message);
		}

		#endregion

		#region Event handlers

		private void _HeartbeatTimer_Callback(object state)
		{
			Heartbeat(HeartbeatTimeout);
		}

		private void _TelemetryTimer_Callback(object state)
		{
			try
			{
				GetTelemetry();
			}
			catch (Exception ex)
			{
				OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
			}
		}

		private void _StationStateTimer_Callback(object state)
		{
			try
			{
				if (StationStateFollowsNearest && _NearestSet)
				{
					_StationStateCoasterId = _NearestCoasterId;
					_StationStateStationId = _NearestStationId;
				}
				if (_StationStateCoasterId > -1 && _StationStateStationId > -1)
				{
					GetStationState(_StationStateCoasterId, _StationStateStationId);
				}
			}
			catch (Exception ex)
			{
				OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
			}
		}

		private void _CurrentCoasterAndStationTimer_Callback(object state)
		{
			OnTrace("Entering...");
			try
			{
				if (!_IsUpdatingNearest) return;
				GetCurrentCoasterAndNearestStation(false);
			}
			catch (Exception ex) {
				OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
			}
			OnTrace("Leaving...");
		}

		private void _SendThread_Loop()
		{
			OnTrace("Entering...");
			CancellationToken cancelToken = _CancelTokenSource.Token;
			while (!cancelToken.IsCancellationRequested)
			{
				try
				{
					OnTrace("Attempting to dequeue message for send...");
					Message message = _Requests.Take(cancelToken);
					OnTrace("Sending message: " + message.MessageType.ToString());
					byte[] payload = message.GetMessagePayload();
					int sent = 0;
					while (sent < payload.Length)
					{
						sent += _Socket.Send(payload, sent, payload.Length - sent, SocketFlags.None);
					}
					_RequestsAwaitingResponses.TryAdd(message.RequestID, message);
				}
				catch (ObjectDisposedException) { } // Ignore, this client is being disposed
				catch (OperationCanceledException) { } // Ignore, this client is being disposed
				catch (Exception ex)
				{
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}
			}
		}

		private void _ReceiveThread_Loop()
		{
			OnTrace("Entering...");
			CancellationToken cancelToken = _CancelTokenSource.Token;
			List<byte> messageBuffer = new List<byte>();
			while (!cancelToken.IsCancellationRequested)
			{
				try
				{
					OnTrace("Attempting to read a message from the socket...");

					// Read until we have a message
					int start;
					int length;
					Message response;
					do
					{
						if (messageBuffer.Count > (ushort.MaxValue * 2))
						{
							throw new InvalidOperationException("Protocol error: server is sending data we don't understand. It is likely not a Telemetry server.");
						}

						byte[] buffer = new byte[1024];
						int rcvd = _Socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

						messageBuffer.AddRange(buffer.Take(rcvd));
					}
					while (!Message.TryParseFromReceivedBytes(messageBuffer.ToArray(), out start, out length, out response));

					OnTrace("Received message: " + response.MessageType.ToString());

					// We now have a message, remove the associated bytes from the buffer
					messageBuffer.RemoveRange(start, length);

					// Process the response on a thread pool thread
					Task.Run(() => ProcessResponse(response));
				}
				catch (OperationCanceledException) { } // Ignore, this client is being disposed
				catch (ObjectDisposedException) { } // Ignore, this client is being disposed
				catch (SocketException ex)
				{
					messageBuffer.Clear();
					string message = string.Format("SocketException({0}): {1} ErrorCode: {2}", ex.SocketErrorCode.ToString(), ex.Message, ex.ErrorCode);
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, message, "SocketException", ex.StackTrace));
				}
				catch (Exception ex)
				{
					messageBuffer.Clear();
					OnError(new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}

			}
		}

		#endregion

		#endregion
	}
}
