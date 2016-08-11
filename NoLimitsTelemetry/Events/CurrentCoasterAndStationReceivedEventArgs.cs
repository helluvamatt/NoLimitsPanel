using NoLimitsTelemetry.Data;

namespace NoLimitsTelemetry.Events
{
	public class CurrentCoasterAndStationReceivedEventArgs : ResponseEventArgs
	{
		internal CurrentCoasterAndStationReceivedEventArgs(uint requestId, CurrentCoasterAndStation data) : base(requestId)
		{
			CurrentCoasterAndStation = data;
		}

		public CurrentCoasterAndStation CurrentCoasterAndStation { get; private set; }
	}
}