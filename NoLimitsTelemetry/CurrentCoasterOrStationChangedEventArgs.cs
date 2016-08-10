using NoLimitsTelemetry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry
{
	public class CurrentCoasterOrStationChangedEventArgs : EventArgs
	{
		public CurrentCoasterOrStationChangedEventArgs(CurrentCoasterAndStation currentCoasterAndStation, string currentCoasterName, Telemetry telemetry)
		{
			CurrentCoasterAndStation = currentCoasterAndStation;
			CurrentCoasterName = currentCoasterName;
			CurrentTelemetry = telemetry;
		}

		public CurrentCoasterAndStation CurrentCoasterAndStation { get; private set; }
		public string CurrentCoasterName { get; private set; }
		public Telemetry CurrentTelemetry { get; private set; }

		public CoasterStyle CurrentCoasterStyle
		{
			get
			{
				return CurrentTelemetry.CoasterStyle;
			}
		}
	}
}
