using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Data
{
	public class CurrentCoasterAndStation
	{
		public CurrentCoasterAndStation(int coaster, int station)
		{
			CurrentCoaster = coaster;
			CurrentStation = station;
		}

		public int CurrentCoaster { get; private set; }
		public int CurrentStation { get; private set; }
	}
}
