using System;

namespace NoLimitsTelemetry.Events
{
	public class OkMessageReceivedEventArgs : ResponseEventArgs
	{
		internal OkMessageReceivedEventArgs(uint requestId) : base(requestId) { }
	}
}