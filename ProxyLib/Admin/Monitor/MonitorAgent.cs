using Amazon;
using Amazon.SimpleNotificationService;
using Cppl.ProxyLib;
using Cppl.ProxyLib.Batching;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyLib.Admin.Monitor
{
	public class MonitorAgent
	{
		static Task _task;

		static MonitorAgent() {
			_task = Task.Run(AgentProdedure);
		}

		static TimeSpan SAMPLE_INTERVAL = TimeSpan.FromSeconds(10);

		public static Cppl.ProxyLib.Arn MonitorTopicArn { get; set; }
		public static CancellationTokenSource Cts { get; set; }

		static AmazonSimpleNotificationServiceClient _sns = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USWest2);

		static Usage _usage = new Usage();

		static long _packets = 0L;
		static long _dropped = 0L;

		/// <summary>
		/// Checks that the background task is still running, for diagnostics.
		/// </summary>
		public static bool IsRunning { get => _task.Status == TaskStatus.Running; }

		/// <summary>
		/// Usage information is used to drive billing
		/// </summary>
		/// <param name="remote">The external endpoint (ingress traffic arrives from).</param>
		/// <param name="local">The internal endpoint (ingress traffic arrives at).</param>
		/// <param name="bytes">The number of bytes of packet data (careful not to include header or envelope data that we add)</param>
		/// <param name="packets">The number of packets handled (UDP) or messages parsed from or sent to a TCP/TLS stream.</param>
		public static void RecordIngress(IPEndPoint remote, IPEndPoint local, long bytes, long packets) {
			RecordUsage(remote, local, Usage.TrafficDirectionType.Ingress, bytes, packets);
		}

		public static void RecordEgress(IPEndPoint remote, IPEndPoint local, long bytes, long packets) {
			RecordUsage(remote, local, Usage.TrafficDirectionType.Egress, bytes, packets);
		}

		private static void RecordUsage(IPEndPoint remote, IPEndPoint local, Usage.TrafficDirectionType direction, long bytes, long packets) {
			var usage = _usage;
			while (!usage.Details.ContainsKey((remote, local)) &&
				!usage.Details.TryAdd((remote, local), new ConcurrentDictionary<Usage.TrafficDirectionType, Usage.TrafficAmounts>())) 
			{ }

			while (!usage.Details[(remote, local)].ContainsKey(direction) && 
				!usage.Details[(remote, local)].TryAdd(direction, new Usage.TrafficAmounts()))
			{ }

			// NOTE: Potential for missing some bytes/packets here if _usage is exchanged during the while above
			// and these lines executed *after* the usage record has been sent.
			var u = usage.Details[(remote, local)][direction];
			u.Bytes += bytes;
			u.Packets += packets;
		}

		/// <summary>
		/// Background task for periodically sending metrics to monitor topic. Careful that the monitor topic
		/// may experience transient issues, and can be changed dynamically by the supervisor.
		/// </summary>
		/// <returns>Task to be awaited, if needed.</returns>
		private static async Task AgentProdedure() {
			var sw = new Stopwatch();
			
			while (Cts?.IsCancellationRequested != true) {
				sw.Restart();
				try {
					if (MonitorTopicArn != null) {

						// 1. let the supervisor know we're here...
						var heartbeat = new HeartBeat() {
							When = DateTime.UtcNow,
							MachineName = Environment.MachineName
						};
						await _sns.PublishAsync(MonitorTopicArn.ToString(), JsonConvert.SerializeObject(heartbeat));

						// 2. send out basic performance metrics
						var network = NetworkInterface.GetAllNetworkInterfaces().Select(n => n.GetIPv4Statistics()).ToArray();
						var performance = new Performance() {
							ListenerCount = 0,
							DestinationCount = BatcherFactory.QueueCount,
							ActiveDestinations = BatcherFactory.ActiveQueueCount,
							BackloggedDestinations = BatcherFactory.BackloggedQueueCount,
							TotalBacklogLength = BatcherFactory.TotalQueueLength,
							MsgsReceivedRate = 0,
							MsgsSentRate = BatcherFactory.TotalRate,
							IncommingPacketRate = Interlocked.Exchange(ref _packets, (long)((network.Sum(n => n.UnicastPacketsReceived) - _packets) / SAMPLE_INTERVAL.TotalSeconds)),
							IncommingPacketDropRate = Interlocked.Exchange(ref _dropped, (long)((network.Sum(n => n.IncomingPacketsDiscarded) - _dropped) / SAMPLE_INTERVAL.TotalSeconds))
						};
						await _sns.PublishAsync(MonitorTopicArn.ToString(), JsonConvert.SerializeObject(performance));

						// 3. send out usage data
						var usage = Interlocked.Exchange(ref _usage, new Usage());
						usage.WhenEnd = DateTime.UtcNow;
						await _sns.PublishAsync(MonitorTopicArn.ToString(), JsonConvert.SerializeObject(usage));
					}
				} catch (Exception e) {
					await Console.Out.WriteLineAsync($"Monitor agent exception: {e.ToString()}");
				}

				// try to hit the right interval for messages
				var delay = SAMPLE_INTERVAL - sw.Elapsed;
				if (delay > TimeSpan.Zero)
					await Task.Delay(delay);
			}
		}
	}
}
