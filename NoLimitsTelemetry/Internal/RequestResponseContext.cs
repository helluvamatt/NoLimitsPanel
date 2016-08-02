using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Internal
{
	internal class RequestResponseContext
	{
		public Message Request { get; private set; }

		private object responseLockObj = new object();
		public Message Response { get; private set; }

		private AutoResetEvent _Trigger;

		public RequestResponseContext(Message request)
		{
			_Trigger = new AutoResetEvent(false);
			Request = request;
		}

		public void Wait(TimeSpan timeout)
		{
			_Trigger.WaitOne(timeout);
		}

		public void ResponseArrived(Message response)
		{
			lock (responseLockObj)
			{
				Response = response;
				_Trigger.Set();
			}
		}
	}
}
