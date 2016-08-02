using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Data
{
	[Flags]
	public enum StationState
	{
		// TODO Need to verify this with NoLimits actual value sent back
		EmergencyStop = 1,
		ManualDispatch = 2,
		CanDispatch = 4,
		CanCloseGates = 8,
		CanOpenGates = 16,
		CanCloseHarness = 32,
		CanOpenHarness = 64,
		CanRaisePlatform = 128,
		CanLowerPlatform = 256,
		CanLockFlyerCar = 512,
		CanUnlockFlyerCar = 1024,
	}
}
