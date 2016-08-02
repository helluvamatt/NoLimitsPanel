using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry
{
	public class ErrorEventArgs : EventArgs
	{
		internal ErrorEventArgs(ErrorType type, string message, string exceptionType, string exceptionStackTrace)
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
