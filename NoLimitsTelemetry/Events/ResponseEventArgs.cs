using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoLimitsTelemetry.Events
{
	public abstract class ResponseEventArgs
	{
		internal ResponseEventArgs(uint? requestId)
		{
			RequestId = requestId;
		}

		public uint? RequestId { get; private set; }
	}
}
