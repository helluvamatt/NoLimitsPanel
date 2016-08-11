namespace NoLimitsTelemetry.Events
{
	public class CoasterNameReceivedEventArgs : ResponseEventArgs
	{
		internal CoasterNameReceivedEventArgs(uint requestId, int index, string name) : base(requestId)
		{
			CoasterName = name;
			CoasterIndex = index;
		}

		public string CoasterName { get; private set; }

		public int CoasterIndex { get; private set; }
	}
}