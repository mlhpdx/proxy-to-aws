using System;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	public interface IProxyDestination
	{
		Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages);
	}
}
