using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace ProxyLib.Admin.Monitor
{
	class Usage : BaseMonitorMessage<Usage>
	{
		/*TrafficDirectionType values 2 and 4 reserved for refinement of ingress (TLS may be special) */
		[Flags] public enum TrafficDirectionType { Unknown = 0, Ingress = 1, Egress = 8 }
		public class TrafficAmounts
		{
			public long Bytes { get; set; } = 0L;
			public long Packets { get; set; } = 0L;
		}

		public DateTime? WhenEnd { get; set; }
		public ConcurrentDictionary<(IPEndPoint remote, IPEndPoint local), ConcurrentDictionary<TrafficDirectionType, TrafficAmounts>> Details { get; set; }
			= new ConcurrentDictionary<(IPEndPoint remote, IPEndPoint local), ConcurrentDictionary<TrafficDirectionType, TrafficAmounts>>();
	}
}
