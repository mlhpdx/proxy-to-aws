using Cppl.ProxyLib.Destinations;
using ProxyLib.Admin.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Listeners
{
	static class UdpClientExtensions
	{
		internal static UdpClient WithSendBuffer(this UdpClient c, int size) { c.Client.SendBufferSize = size; return c; }
		internal static UdpClient WithReceiveBuffer(this UdpClient c, int size) { c.Client.ReceiveBufferSize = size; return c; }
	}

	class UdpListener : IListener, IDisposable
	{
		UdpClient _client;

		internal UdpListener(IPEndPoint endpoint) {
			_client = new UdpClient(endpoint).WithReceiveBuffer(2 * 1024 * 1024).WithSendBuffer(2 * 1024 * 1024);
		}

		public IProxyDestination Destination { get; set; }
		public IClientNetworkRestriction ClientRestrictions { get; set; }

		public async Task ResponseCallback(IEnumerable<Packet> replies) {
			await Task.WhenAll(replies?.Where(r => r.Data != null)
				.Select(r => {
					MonitorAgent.RecordEgress(r.Remote, r.Local, r.Data.Length, 1);
					return _client.SendAsync(r.Data, r.Data.Length, r.Remote);
				}));
		}

		public async Task Listen(CancellationTokenSource cts = null) {
			await Console.Out.WriteLineAsync($"Starting to Listen for UDP on {_client.Client.LocalEndPoint}.");
			var local = _client.Client.LocalEndPoint as IPEndPoint;
			while (cts?.IsCancellationRequested != true) {
				try {
					var received = await _client.ReceiveAsync();
					var remote = received.RemoteEndPoint;

					MonitorAgent.RecordIngress(remote, local, received.Buffer.Length, 1);

					if (ClientRestrictions?.IsAllowed(remote.Address) != true || Destination == null)
						continue; // ignored

					await Destination.AcceptMessages(ResponseCallback, new PacketInfo() {
						From = new PacketInfo.EndpointInfo() {
							IpAddress = remote.Address.ToString(),
							Port = remote.Port
						},
						Received = DateTime.UtcNow,
						Receiver = new PacketInfo.EndpointInfo() {
							IpAddress = local.Address.ToString(),
							Port = local.Port
						},
						Base64Packet = Convert.ToBase64String(received.Buffer)
					});
				} catch (Exception e) {
					Console.WriteLine($"Failed to handle UDP packet on {_client.Client.LocalEndPoint}: {e.Message}.");
				}
			}
		}

		public void Dispose() {
			_client?.Dispose();
		}
	}
}
