using Cppl.ClientPolicyFramework;
using DnsClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cppl.ProxyLib
{
	public class ClientNetworkRestriction : IClientNetworkRestriction
	{
		// provides quick answers on repeat clients
		private static ConcurrentDictionary<IPAddress, (DateTime ts, bool answer)> _ipcache =
			new ConcurrentDictionary<IPAddress, (DateTime ts, bool answer)>();

		private static Task _cleanup = Task.Run(async () => {
			while (true) {
				var expired = _ipcache.Where(kv => kv.Value.ts < DateTime.UtcNow.AddSeconds(30));
				var remove = expired.Select(kv => kv.Key).ToArray();
				foreach (var ip in remove)
					_ipcache.TryRemove(ip, out var _);
				await Task.Delay(1000);
			}
		});

		// provides DNS lookup for IP addresses to allow as clients, with caching
		private static LookupClient _lookup = new LookupClient() {
			UseCache = true,
			MinimumCacheTimeout = TimeSpan.FromSeconds(30)
		};

		public bool IsAllowed(IPAddress client) {
			// Four stages to check:
			// 1) the in-memory cache, stop if found and give the cached answer
			if (_ipcache.ContainsKey(client))
				return _ipcache[client].answer;

			// 2) the AllowedNetwork CIDRs, stop if matched
			if (AllowedNetworks?.Any(n => n.Contains(client)) == true)
				return (_ipcache[client] = (DateTime.UtcNow, true)).answer;

			// 3) the AllowedDomains:
			//    a) if a client policy framework is found for a domain, stop if matched against it
			//    b) otherwise, stop if matched against the A records for that domain
			foreach (var domain in AllowedDomains) {
				var policy = Policy.GetDomainPolicy(domain);
				var answer = (policy?.RootRecord?.Mechanisms?.Any(m => policy.IsAllowed(client, m)) ?? // a
					_lookup.GetHostEntry(domain).AddressList.Contains(client)) == true; // b
				if (answer)
					return (_ipcache[client] = (DateTime.UtcNow, true)).answer;
			}
			// 4) fail
			return (_ipcache[client] = (DateTime.UtcNow, false)).answer;
		}

		public bool IsAllowed(X509Certificate crt) =>
			new X509Certificate2(crt).Verify()
			&& (TlsClientCertificates?.Any() == false
				|| TlsClientCertificates.Any(c => c.GetCertHashString() == crt.GetCertHashString()));

		public IEnumerable<IPNetwork> AllowedNetworks { get; set; }
		public IEnumerable<string> AllowedDomains { get; set; }

		public IEnumerable<X509Certificate2> TlsClientCertificates { get; set; }
	}
}
