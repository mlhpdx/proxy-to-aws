using Newtonsoft.Json;
using System;

namespace Cppl.ProxyLib
{
	public class PacketInfo
	{
		public class EndpointInfo
		{
			public string IpAddress { get; set; }
			public int Port { get; set; }
		}

		public DateTime Received { get; set; }
		public EndpointInfo From { get; set; }
		public EndpointInfo Receiver { get; set; }
		[JsonProperty("Packet")] // TODO: remove once I deploy the Lambda using the Base64Packet name.
		public string Base64Packet { get; set; }

		public override string ToString() {
			return JsonConvert.SerializeObject(this, Formatting.None);
		}
	}
}
