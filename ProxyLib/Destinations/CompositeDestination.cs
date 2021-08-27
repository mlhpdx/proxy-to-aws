using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class CompositeDestination : IProxyDestination, IDisposable
	{
		string[] _names;
		public string[] Names {
			get { return _names; }
			set { _names = value; Reset(); }
		}

		Func<string, IProxyDestination> _resolver;
		IDictionary<string, IProxyDestination> _destinations;

		internal CompositeDestination(Func<string, IProxyDestination> resolver) {
			_resolver = resolver;
			Reset();
		}

		public void Reset() {
			_destinations = _names?.ToDictionary(n => n, n => _resolver(n));
		}

		public async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			await Task.WhenAll(_destinations.Select(d => d.Value.AcceptMessages(callback, messages)));
		}

		public void Dispose() {
			foreach (IDisposable d in _destinations.Values)
				d.Dispose();
		}
	}
}
