using NoLimitsTelemetry.Data;
using System;

namespace NoLimitsTelemetry.Events
{
	public class CurrentCoasterOrStationChangedEventArgs : EventArgs
	{
		internal CurrentCoasterOrStationChangedEventArgs(CurrentCoasterAndStation currentCoasterAndStation)
		{
			CurrentCoasterAndStation = currentCoasterAndStation;
		}

		public CurrentCoasterAndStation CurrentCoasterAndStation { get; private set; }
	}
}
