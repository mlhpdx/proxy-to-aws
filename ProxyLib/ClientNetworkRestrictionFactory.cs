using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Cppl.ProxyLib
{
	public static class ClientNetworkRestrictionFactory
	{
		public static IClientNetworkRestriction Get(ProxyConfig.ClientNetworkRestriction clients) {
			var store = new X509Store(StoreLocation.CurrentUser);
			store.Open(OpenFlags.ReadOnly);
			return new ClientNetworkRestriction() {
				AllowedDomains = clients.AllowedDomains,
				AllowedNetworks = clients.AllowedNetworks,
				TlsClientCertificates = clients.TlsClientCertificates?.Select(name =>
					store.Certificates.Find(X509FindType.FindBySubjectName, name, false)
					.Cast<X509Certificate2>().FirstOrDefault()).ToArray()
			};
		}
	}
}
