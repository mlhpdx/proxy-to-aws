using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cppl.ProxyLib.Destinations;

namespace Cppl.ProxyLib.Listeners
{
	public static class ListenerExtensions
	{
		public static IListener WithDestination(this IListener listener, IProxyDestination destination) {
			listener.Destination = destination;
			return listener;
		}
		public static IEnumerable<IListener> WithDestination(this IEnumerable<IListener> listeners, IProxyDestination destination) {
			return listeners.Select(l => l.WithDestination(destination));
		}
		public static IListener WithClientNetworks(this IListener listener, IClientNetworkRestriction restrictions) {
			listener.ClientRestrictions = restrictions;
			return listener;
		}
		public static IEnumerable<IListener> WithClientNetworks(this IEnumerable<IListener> listeners, IClientNetworkRestriction restrictions) {
			return listeners.Select(l => l.WithClientNetworks(restrictions));
		}
	}
	public interface IListener {
		IProxyDestination Destination { get; set; }
		IClientNetworkRestriction ClientRestrictions { get; set; }
		Task Listen(CancellationTokenSource cts = default);
	}
}
