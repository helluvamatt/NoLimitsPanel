using NoLimitsTelemetry.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Data
{
	public class Telemetry
	{
		internal Telemetry(byte[] data)
		{
			if (data.Length != 76) throw new ArgumentOutOfRangeException("data", "Must be exactly 76 bytes. Got: " + data.Length + " bytes");
			State = (TelemetryState)Utils.GetInt32(data, 0);
			Frame = Utils.GetInt32(data, 4);
			ViewMode = Utils.GetInt32(data, 8);
			CurrentCoaster = Utils.GetInt32(data, 12);
			CoasterStyle = (CoasterStyle)Utils.GetInt32(data, 16);
			CurrentTrain = Utils.GetInt32(data, 20);
			CurrentCar = Utils.GetInt32(data, 24);
			CurrentSeat = Utils.GetInt32(data, 28);
			Speed = Utils.GetFloat32(data, 32);
			PositionX = Utils.GetFloat32(data, 36);
			PositionY = Utils.GetFloat32(data, 40);
			PositionZ = Utils.GetFloat32(data, 44);
			RotationQuaternionX = Utils.GetFloat32(data, 48);
			RotationQuaternionY = Utils.GetFloat32(data, 52);
			RotationQuaternionZ = Utils.GetFloat32(data, 56);
			RotationQuaternionW = Utils.GetFloat32(data, 60);
			GForceX = Utils.GetFloat32(data, 64);
			GForceY = Utils.GetFloat32(data, 68);
			GForceZ = Utils.GetFloat32(data, 72);
		}

		public TelemetryState State { get; private set; }
		public int Frame { get; private set; }
		public int ViewMode { get; private set; }
		public int CurrentCoaster { get; private set; }
		public CoasterStyle CoasterStyle { get; private set; }
		public int CurrentTrain { get; private set; }
		public int CurrentCar { get; private set; }
		public int CurrentSeat { get; private set; }
		public float Speed { get; private set; }
		public float PositionX { get; private set; }
		public float PositionY { get; private set; }
		public float PositionZ { get; private set; }
		public float RotationQuaternionX { get; private set; }
		public float RotationQuaternionY { get; private set; }
		public float RotationQuaternionZ { get; private set; }
		public float RotationQuaternionW { get; private set; }
		public float GForceX { get; private set; }
		public float GForceY { get; private set; }
		public float GForceZ { get; private set; }

		[Flags]
		public enum TelemetryState
		{
			// TODO Need to verify this with NoLimits actual value sent back
			InPlayMode = 1,
			Braking = 2,
		}
	}
}
