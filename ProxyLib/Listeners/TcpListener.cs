using Cppl.ProxyLib.Destinations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Listeners
{
	class TcpListener : IListener
	{
		protected class TcpLooper
		{
			TcpListener _listener;
			TcpClient _client;
			IPEndPoint _remote;
			IPEndPoint _local;
			Stream _socket;

			internal TcpLooper(TcpListener listener, TcpClient client) {
				_listener = listener;
				_client = client;
				_socket = client.GetStream();
				_remote = client.Client.RemoteEndPoint as IPEndPoint;
				_local = client.Client.LocalEndPoint as IPEndPoint;
			}

			internal async Task Loop(CancellationTokenSource cts = null) {
				try {
					while (cts?.IsCancellationRequested != true && _client.Connected && _client.Client.Connected) {
						var payload = await _listener._splitter.GetNextSegment(_socket);
						if (payload?.Length > 0 && _listener.Destination != null) {
							await _listener.Destination.AcceptMessages(ResponseCallback, new PacketInfo() {
								From = new PacketInfo.EndpointInfo() {
									IpAddress = _remote.Address.ToString(),
									Port = _remote.Port
								},
								Received = DateTime.UtcNow,
								Receiver = new PacketInfo.EndpointInfo() {
									IpAddress = _local.Address.ToString(),
									Port = _local.Port
								},
								Base64Packet = Convert.ToBase64String(payload)
							});
						} else break;
					}
				} catch (Exception e) {
					await Console.Out.WriteLineAsync($"Failure in tcp looper for {_remote}: {e.Message}");
				} finally {
					// await Console.Out.WriteLineAsync($"Exiting looper for {_remote}.");
					_socket.Dispose();
					_client.Dispose();
				}
			}

			public async Task ResponseCallback(IEnumerable<Packet> replies) {
				await Task.WhenAll(replies?.Where(r => r.Data != null).ToList().Select(r =>
					_socket.WriteAsync(r.Data, 0, r.Data.Length)));
			}
		}

		System.Net.Sockets.TcpListener _server;
		IStreamSplitter _splitter;
		ConcurrentDictionary<TcpLooper, Task> _loopers = new ConcurrentDictionary<TcpLooper, Task>();

		internal TcpListener(IPEndPoint endpoint, IStreamSplitter splitter) {
			_server = new System.Net.Sockets.TcpListener(endpoint);
			_splitter = splitter;
		}

		public IProxyDestination Destination { get; set; }
		public IClientNetworkRestriction ClientRestrictions { get; set; } // TODO: when set, close any existing loopers that are not allowed under the new rule

		public async Task Listen(CancellationTokenSource cts = null) {
			await Console.Out.WriteLineAsync($"Starting to Listen for TCP on {_server.LocalEndpoint}.");
			try {
				cts?.Token.Register(() => _server.Stop());
				_server.Start();
				int delay = 10;
				while (cts?.IsCancellationRequested != true) {
					try {
						var client = await _server.AcceptTcpClientAsync();

						if (ClientRestrictions?.IsAllowed((client.Client.RemoteEndPoint as IPEndPoint).Address) != true) {
							await Console.Out.WriteLineAsync($"Rejected new connection from {client.Client.RemoteEndPoint} on {client.Client.LocalEndPoint}.");
							client.Close();
							continue; // ignored
						} else {
//							await Console.Out.WriteLineAsync($"Accepted new connection from {client.Client.RemoteEndPoint} on {client.Client.LocalEndPoint}.");
							var looper = GetLooper(client);
							Task.Run(() => looper.Loop()); // runs independently
						}
						delay = 100;
					} catch (Exception e) {
						if (delay > 10000)
							throw;
						await Task.Delay(delay *= 10);
					}
				}
			} catch (Exception e) {
				await Console.Error.WriteLineAsync($"Exception in TCP listener for {_server.LocalEndpoint}: {e.Message}\n{e.ToString()}");
			} finally {
				await Console.Out.WriteLineAsync($"No longer listening on {_server.LocalEndpoint}.");
			}
		}

		protected virtual TcpLooper GetLooper(TcpClient client) {
			return new TcpLooper(this, client);
		}
	}
}