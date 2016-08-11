using System;

namespace NoLimitsTelemetry.Events
{
	public class VersionReceivedEventArgs : ResponseEventArgs
	{
		internal VersionReceivedEventArgs(uint requestId, Version version) : base(requestId)
		{
			Version = version;
		}

		public Version Version { get; private set; }
	}
}