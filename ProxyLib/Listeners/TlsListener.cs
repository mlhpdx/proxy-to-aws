using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Cppl.ProxyLib.Listeners
{
	class TlsListener : TcpListener
	{
		internal TlsListener(IPEndPoint endpoint, IStreamSplitter splitter) : base(endpoint, splitter) {
		}

		public X509Certificate2 TlsServerCertificate { get; set; }

		protected override TcpLooper GetLooper(TcpClient client) {
			Console.Out.WriteLine($"Starting TLS on {client.Client.LocalEndPoint}.");

			RemoteCertificateValidationCallback remote = (sender, crt, chain, errors) 
				=> ClientRestrictions?.IsAllowed(crt) == true;
			LocalCertificateSelectionCallback local = (sender, host, l, r, issuers) 
				=> TlsServerCertificate;

			var stream = new SslStream(client.GetStream(), false, remote, local);
			stream.AuthenticateAsServer(TlsServerCertificate, false, SslProtocols.Tls12, false);

			return new TcpLooper(this, client);
		}
	}
}
