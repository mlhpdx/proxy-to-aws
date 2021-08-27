using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Cppl.ProxyLib
{
	public interface IClientNetworkRestriction
	{
		bool IsAllowed(IPAddress client);
		bool IsAllowed(X509Certificate crt);
	}
}