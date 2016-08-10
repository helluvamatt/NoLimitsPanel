using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NoLimitsTelemetry;
using Microsoft.AspNet.SignalR.Hubs;
using NoLimitsPanel.Services;

namespace NoLimitsPanel.Hubs
{
	[HubName(HUB_NAME)]
	public class TelemetryControlHub : Hub<ITelemetryControl>
	{
		public const string HUB_NAME = "telemetryControlHub";

		public async override Task OnDisconnected(bool stopCalled)
		{
			await DisconnectTelemetry();
			await base.OnDisconnected(stopCalled);
		}

		public async Task<bool> ConnectTelemetry(string host, int port)
		{
			await DisconnectTelemetry();
			return await TelemetryService.Current.CreateClient(Context.ConnectionId, host, port);
		}

		public async Task<bool> CheckConnection()
		{
			return await DoAsync((client) => client.Connected);
		}

		public async Task<bool> SetGates(int coasterIndex, int stationIndex, bool open)
		{
			return await DoAsync((client) => client.SetGates(coasterIndex, stationIndex, open));
		}

		public async Task<bool> SetHarness(int coasterIndex, int stationIndex, bool open)
		{
			return await DoAsync((client) => client.SetHarness(coasterIndex, stationIndex, open));
		}

		public async Task<bool> SetPlatform(int coasterIndex, int stationIndex, bool lowered)
		{
			return await DoAsync((client) => client.SetPlatform(coasterIndex, stationIndex, lowered));
		}

		public async Task<bool> SetFlyerCar(int coasterIndex, int stationIndex, bool locked)
		{
			return await DoAsync((client) => client.SetFlyerCar(coasterIndex, stationIndex, locked));
		}

		public async Task<bool> SetEmergencyStop(bool estop)
		{
			return await DoAsync((client) => client.SetEmergencyStop(estop));
		}

		public async Task<bool> SetManualMode(int coasterIndex, int stationIndex, bool manualMode)
		{
			return await DoAsync((client) => client.SetManualMode(coasterIndex, stationIndex, manualMode));
		}

		public async Task<bool> Dispatch(int coasterIndex, int stationIndex)
		{
			return await DoAsync((client) => client.Dispatch(coasterIndex, stationIndex));
		}

		public async Task DisconnectTelemetry()
		{
			await TelemetryService.Current.DestroyClient(Context.ConnectionId);
		}

		private async Task<TRet> DoAsync<TRet>(Func<NoLimitsTelemetryClient, TRet> action)
		{
			return await TelemetryService.Current.DoAsync(action, Context.ConnectionId);
		}
	}

	public interface ITelemetryControl
	{
		void OnError(ErrorEventArgs error);
		void OnTelemetryReceived(TelemetryReceivedEventArgs telemetry);
		void OnStationStateReceived(StationStateReceivedEventArgs stationState);
		void OnCurrentCoasterOrStationChanged(CurrentCoasterOrStationChangedEventArgs args);
	}
}