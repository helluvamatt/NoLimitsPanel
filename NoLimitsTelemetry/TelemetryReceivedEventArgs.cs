using NoLimitsTelemetry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry
{
	public class TelemetryReceivedEventArgs : EventArgs
	{
		public Telemetry TelemetryData { get; set; }
	}
}
