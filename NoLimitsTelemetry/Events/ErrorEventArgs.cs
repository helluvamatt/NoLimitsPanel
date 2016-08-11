namespace NoLimitsTelemetry.Events
{
	public class ErrorEventArgs : ResponseEventArgs
	{
		public ErrorEventArgs(ErrorType type, string message, string exceptionType, string exceptionStackTrace) : this(null, type, message, exceptionType, exceptionStackTrace) { }

		internal ErrorEventArgs(uint? requestId, ErrorType type, string message, string exceptionType, string exceptionStackTrace) : base(requestId)
		{
			Type = type;
			ErrorMessage = message;
			ExceptionType = exceptionType;
			ExceptionStackTrace = exceptionStackTrace;
		}

		public string ErrorMessage { get; private set; }

		public string ExceptionType { get; private set; }

		public string ExceptionStackTrace { get; private set; }

		public ErrorType Type { get; private set; }

		public enum ErrorType
		{
			Server, Client
		}
	}


}
