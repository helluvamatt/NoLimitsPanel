using Microsoft.AspNet.SignalR;
using NoLimitsPanel.Hubs;
using NoLimitsTelemetry;
using NoLimitsTelemetry.Events;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NoLimitsPanel.Services
{
	internal class TelemetryService
	{
		private ConcurrentDictionary<string, TelemetryClientInstance> _Instances = new ConcurrentDictionary<string, TelemetryClientInstance>();

		public async Task<bool> CreateClient(string id, string host, int port)
		{
			try
			{
				TelemetryClientInstance newInstance = new TelemetryClientInstance(id, host, port);
				_Instances[id] = newInstance;
				return await newInstance.ConnectAsync();
			}
			catch (Exception ex)
			{
				OnError(id, new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				return false;
			}
		}

		// I think this qualifies as "cool code"
		public async Task<TRet> DoAsync<TRet>(Func<NoLimitsTelemetryClient, TRet> action, string clientId)
		{
			return await Task.Run(() =>
			{
				try
				{
					return action(_Instances[clientId].Client);
				}
				catch (Exception ex)
				{
					OnError(clientId, new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}
				return default(TRet);
			});
		}

		public async Task DestroyClient(string id)
		{
			await Task.Run(() =>
			{
				try
				{
					TelemetryClientInstance instance;
					if (_Instances.TryRemove(id, out instance))
					{
						instance.Dispose();
					}
				}
				catch (Exception ex)
				{
					OnError(id, new ErrorEventArgs(ErrorEventArgs.ErrorType.Client, ex.Message, ex.GetType().Name, ex.StackTrace));
				}
			});
		}

		private void OnError(string clientId, ErrorEventArgs args)
		{
			GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(clientId).OnError(args);
		}

		#region Singleton pattern

		private static TelemetryService _Current;
		private static object _CurrentLockObj = new { };
		public static TelemetryService Current
		{
			get
			{
				lock (_CurrentLockObj)
				{
					if (_Current == null)
					{
						_Current = new TelemetryService();
					}
					return _Current;
				}
			}
		}

		#endregion

		#region Support classes

		internal class TelemetryClientInstance : IDisposable
		{
			#region Private variables

			private string _ClientID;

			#endregion

			#region Properties

			public NoLimitsTelemetryClient Client { get; private set; }

			#endregion

			#region Constructor

			public TelemetryClientInstance(string id, string host, int port)
			{
				_ClientID = id;
				Client = new NoLimitsTelemetryClient(host, port);
				Client.Error += Client_Error;
				Client.OkMessageReceived += Client_OkMessageReceived;
				Client.VersionReceived += Client_VersionReceived;
				Client.CoasterCountReceived += Client_CoasterCountReceived;
				Client.CoasterNameReceived += Client_CoasterNameReceived;
				Client.CurrentCoasterAndStationReceived += Client_CurrentCoasterAndStationReceived;
				Client.TelemetryReceived += Client_TelemetryReceived;
				Client.StationStateReceived += Client_StationStateReceived;
				Client.CurrentCoasterOrStationChanged += Client_CurrentCoasterOrStationChanged;
				Client.StationStateFollowsNearest = true;
			}

			#endregion

			#region Public methods

			public async Task<bool> ConnectAsync()
			{
				await Task.Run(() => Client.Connect());
				Client.StartCurrentCoasterAndNearestStation(TimeSpan.FromMilliseconds(500));
				Client.StartTelemetry(TimeSpan.FromMilliseconds(250));
				Client.StartStationState(TimeSpan.FromMilliseconds(250), 0, 0);
				return Client.Connected;
			}

			public void Dispose()
			{
				if (Client != null) Client.Dispose();
			}

			#endregion

			#region Event handlers

			private void Client_Error(object sender, ErrorEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnError(e);
			}

			private void Client_OkMessageReceived(object sender, OkMessageReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnOkMessage(e);
			}

			private void Client_VersionReceived(object sender, VersionReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnVersionReceieved(e);
			}

			private void Client_StationStateReceived(object sender, StationStateReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnStationStateReceived(e);
			}

			private void Client_TelemetryReceived(object sender, TelemetryReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnTelemetryReceived(e);
			}

			private void Client_CoasterNameReceived(object sender, CoasterNameReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnCoasterNameReceived(e);
			}

			private void Client_CoasterCountReceived(object sender, CoasterCountReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnCoasterCountReceived(e);
			}

			private void Client_CurrentCoasterAndStationReceived(object sender, CurrentCoasterAndStationReceivedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnCurrentCoasterAndStationReceived(e);
			}

			private void Client_CurrentCoasterOrStationChanged(object sender, CurrentCoasterOrStationChangedEventArgs e)
			{
				GlobalHost.ConnectionManager.GetHubContext<ITelemetryControl>(TelemetryControlHub.HUB_NAME).Clients.Client(_ClientID).OnCurrentCoasterOrStationChanged(e);
			}

			#endregion
		}

		#endregion
	}
}