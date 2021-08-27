using System.Net;

namespace Cppl.ProxyLib
{
	public class Packet
	{
		public IPEndPoint Local { get; set; }
		public IPEndPoint Remote { get; set; }
		public byte[] Data { get; set; }
	}
}
