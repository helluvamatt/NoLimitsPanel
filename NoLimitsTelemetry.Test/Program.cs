using Newtonsoft.Json;
using NoLimitsTelemetry.Data;
using NoLimitsTelemetry.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Starting...");
			NoLimitsTelemetryClient client = new NoLimitsTelemetryClient("localhost", 15151);
			client.TraceHandler = new TraceHandler();
			client.Error += Client_Error;
			client.TelemetryReceived += Client_TelemetryReceived;
			client.StationStateReceived += Client_StationStateReceived;
			client.CurrentCoasterOrStationChanged += Client_CurrentCoasterOrStationChanged;
			client.StationStateFollowsNearest = true;
			Console.WriteLine("Connecting...");
			client.Connect();
			Console.WriteLine("Connected to NoLimits version: {0}", client.GetVersion());
			client.StartCurrentCoasterAndNearestStation(TimeSpan.FromMilliseconds(2000));
			client.StartTelemetry(TimeSpan.FromMilliseconds(500));
			client.StartStationState(TimeSpan.FromMilliseconds(1000), 0, 0);
			int countdown = 30;
			while (countdown > 0)
			{
				Console.WriteLine("Closing connection in: {0} seconds.", countdown);
				Thread.Sleep(1000);
				countdown--;
			}
			client.Dispose();
		}

		private static void Client_CurrentCoasterOrStationChanged(object sender, CurrentCoasterOrStationChangedEventArgs e)
		{
			Console.WriteLine("[CurrentCoasterOrStationChanged] Coaster Id: {0} Station: {1}", e.CurrentCoasterAndStation.CurrentCoaster, e.CurrentCoasterAndStation.CurrentStation);
		}

		private static void Client_StationStateReceived(object sender, StationStateReceivedEventArgs e)
		{
			Console.WriteLine("[StationStateReceived]");
			Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented));
		}

		private static void Client_TelemetryReceived(object sender, TelemetryReceivedEventArgs e)
		{
			Console.WriteLine("[TelemetryReceived]");
			Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented));
		}

		private static void Client_Error(object sender, ErrorEventArgs e)
		{
			string threadName = Thread.CurrentThread.Name;
			if (string.IsNullOrEmpty(threadName)) threadName = string.Format("Thread {0}", Thread.CurrentThread.ManagedThreadId);

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("[{0}] ERROR", threadName);
			Console.WriteLine();
			Console.WriteLine("[{0}] Message: {1}", threadName, e.ErrorMessage);
			Console.WriteLine("[{0}] Type: {1}", threadName, e.Type.ToString());
			Console.WriteLine("[{0}] Exception Type: {1}", threadName, e.ExceptionType);
			Console.WriteLine("[{0}] StackTrace: {1}", threadName, e.ExceptionStackTrace);
			Console.WriteLine();
			Console.WriteLine();
		}

		class TraceHandler : ITraceHandler
		{
			public void TraceMsg(string threadName, string methodName, string msg)
			{
				Console.WriteLine("[{0}] TRACE: [{1}] {2}", threadName, methodName, msg);
			}
		}
	}
}
