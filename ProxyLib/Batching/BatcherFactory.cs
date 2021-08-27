using Cppl.ProxyLib.Destinations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Cppl.ProxyLib.Batching
{
	public static class BatcherFactory
	{
		static ConcurrentBag<Batcher> _all = new ConcurrentBag<Batcher>();
		static Dictionary<Batcher, (TimeSpan timespan, int count)> _lastRates;

		// resets counters
		static Dictionary<Batcher, (TimeSpan timespan, int count)> Snapshot() { return _lastRates = _all.ToDictionary(b => b, b => b.Rate); }
		public static double InstantaniousTotalRate => Snapshot().Sum(_ => _.Value.count / _.Value.timespan.TotalSeconds);

		// const behavior
		public static double TotalRate => _lastRates?.Sum(_ => _.Value.count / _.Value.timespan.TotalSeconds) ?? 0;
		public static int TotalQueueLength => _all.Sum(b => b.QueueLength);
		public static int QueueCount => _all.Count;
		public static int ActiveQueueCount => _lastRates?.Count(_ => _.Value.count > 0) ?? 0;
		public static int BackloggedQueueCount => _all.Count(b => b.QueueLength > 0);

		internal static Batcher GetBatcher(ProxyConfig.Batching batching, IProxyDestination destination, CancellationTokenSource cts) {
			var b = new Batcher(destination, new Batcher.BatchLimits() {
				Count = batching?.Count ?? 100,
				Size = (long)((batching?.SizeInMB * 1024 * 1024) ?? 1000 * 1000),
				Time = TimeSpan.FromSeconds(batching?.TimeoutInSeconds ?? .1)
			}, cts);
			_all.Add(b);
			return b;
		}
	}
}
