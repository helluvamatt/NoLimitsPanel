using NoLimitsTelemetry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Events
{
	public class StationStateReceivedEventArgs : ResponseEventArgs
	{
		internal StationStateReceivedEventArgs(uint requestId, int coasterId, int stationId, StationState state) : base(requestId)
		{
			CoasterId = coasterId;
			StationId = stationId;
			StationState = state;
		}

		public int CoasterId { get; private set; }
		public int StationId { get; private set; }
		public StationState StationState { get; private set; }
	}
}
