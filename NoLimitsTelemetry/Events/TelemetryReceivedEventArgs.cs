using NoLimitsTelemetry.Data;
using System;

namespace NoLimitsTelemetry.Events
{
	public class TelemetryReceivedEventArgs : ResponseEventArgs
	{
		public TelemetryReceivedEventArgs(uint requestId, Telemetry data) : base(requestId)
		{
			TelemetryData = data;
		}

		public Telemetry TelemetryData { get; private set; }
	}
}
