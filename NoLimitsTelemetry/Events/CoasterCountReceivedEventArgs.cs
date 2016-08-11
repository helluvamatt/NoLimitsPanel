namespace NoLimitsTelemetry.Events
{
	public class CoasterCountReceivedEventArgs : ResponseEventArgs
	{
		internal CoasterCountReceivedEventArgs(uint requestId, int data) : base(requestId)
		{
			CoasterCount = data;
		}

		public int CoasterCount { get; private set; }
	}
}