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
	class IcmpListener : IListener, IDisposable
	{
        IPEndPoint _local;
		Socket _client;

		internal IcmpListener(IPEndPoint endpoint) {
            _local = endpoint;
		}

		public IProxyDestination Destination { get; set; }
		public IClientNetworkRestriction ClientRestrictions { get; set; }

		public async Task ResponseCallback(IEnumerable<Packet> replies) {
			await Task.WhenAll(replies?.Where(r => r.Data != null)
				.Select(r => {
					MonitorAgent.RecordEgress(r.Remote, r.Local, r.Data.Length, 1);
                    return Task.FromResult(_client.SendTo(r.Data, r.Remote));
				}));
		}

		public async Task Listen(CancellationTokenSource cts = null) {
			await Console.Out.WriteLineAsync($"Starting to Listen for ICMP on {_local}.");
			
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            _client.Bind(_local);
            _client.IOControl(IOControlCode.ReceiveAll, new byte[] { 1, 0, 0, 0 }, new byte[] { 1, 0, 0, 0 });

            byte[] buffer = new byte[4096];
			while (cts?.IsCancellationRequested != true) {
				try {
                    EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
					var received = new ArraySegment<byte>(buffer, 0, _client.ReceiveFrom(buffer, ref ep));
                    var remote = ep as IPEndPoint;

					MonitorAgent.RecordIngress(remote, _local, received.Count, 1);

					if (ClientRestrictions?.IsAllowed(remote.Address) != true || Destination == null)
						continue; // ignored

					await Destination.AcceptMessages(ResponseCallback, new PacketInfo() {
						From = new PacketInfo.EndpointInfo() {
							IpAddress = remote.Address.ToString(),
							Port = remote.Port
						},
						Received = DateTime.UtcNow,
						Receiver = new PacketInfo.EndpointInfo() {
							IpAddress = _local.Address.ToString(),
							Port = _local.Port
						},
						Base64Packet = Convert.ToBase64String(received.ToArray())
					});
				} catch (Exception e) {
					Console.WriteLine($"Failed to handle UDP packet on {_local}: {e.Message}.");
				}
			}
		}

		public void Dispose() {
			_client?.Dispose();
		}
	}
}
