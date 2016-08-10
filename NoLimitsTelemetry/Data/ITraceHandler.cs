using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Data
{
	public interface ITraceHandler
	{
		void TraceMsg(string threadName, string methodName, string msg);
	}
}
