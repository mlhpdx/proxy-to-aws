using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Cppl.ProxyLib.Listeners
{
	public static class ListenerFactory
	{
		static readonly Dictionary<ProxyConfig.Binding.Protocol, Func<ProxyConfig.Binding, IPEndPoint, IListener>> _constructors =
			new Dictionary<ProxyConfig.Binding.Protocol, Func<ProxyConfig.Binding, IPEndPoint, IListener>>() {
				{ ProxyConfig.Binding.Protocol.ICMP,
					(binding, endpoint) => new IcmpListener(endpoint)
				},
				{ ProxyConfig.Binding.Protocol.UDP,
					(binding, endpoint) => new UdpListener(endpoint)
				},
				{ ProxyConfig.Binding.Protocol.TCP,
					(binding, endpoint) => new TcpListener(endpoint, SplitterFactory.GetSplitter(binding.StreamSplitter))
				},
				{ ProxyConfig.Binding.Protocol.TLS,
					(binding, endpoint) => {
						var l = new TlsListener(endpoint, SplitterFactory.GetSplitter(binding.StreamSplitter));
						var store = new X509Store(StoreLocation.CurrentUser);
						store.Open(OpenFlags.ReadOnly);
						l.TlsServerCertificate = store.Certificates
							.Find(X509FindType.FindBySubjectName, binding.TlsServerCertificate, false)
							.Cast<X509Certificate2>().FirstOrDefault();
						return l;
					}
				}
		};

		public static IEnumerable<IListener> AttachListeners(ProxyConfig.Binding binding) {
			var endpoints = binding.Protocols.SelectMany(protocol => binding.Ports.Values.Select(port =>
				(endpoint: new IPEndPoint(binding.IPAddress, port), protocol)));
			return endpoints.Select(e => _constructors[e.protocol](binding, e.endpoint)).ToArray();
		}
	}
}
