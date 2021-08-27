using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cppl.ProxyLib.Destinations;

namespace Cppl.ProxyLib.Batching
{
	using BatchType = System.Collections.Generic.List<(Func<Packet[], Task> callback, PacketInfo[] messages)>;
	internal class Batcher : IProxyDestination
	{
		internal class BatchLimits
		{
			public int Count { get; set; }
			public long Size { get; set; }
			public TimeSpan Time { get; set; }
		}

		BatchLimits _limits;
		BatchType _batch;

		BlockingCollection<(Func<Packet[], Task> callback, PacketInfo[] messages)> _queue
			= new BlockingCollection<(Func<Packet[], Task> callback, PacketInfo[] messages)>();
		readonly Task _task;
		CancellationTokenSource _cts;

		int _count = 0;
		long _total = 0;

		DateTime _last;

		internal Batcher(IProxyDestination destination, BatchLimits limits, CancellationTokenSource cts = default) {
			Destination = destination;
			_limits = limits;
			_cts = cts ?? new CancellationTokenSource();
			_batch = new BatchType();
			_last = DateTime.Now;
			_task = Task.Run(() => Loop(cts));
		}

		async Task Loop(CancellationTokenSource cts) {
			var sw = new Stopwatch();
			while (!_queue.IsCompleted && cts?.IsCancellationRequested != true) {
				try {
					var c = 0;
					sw.Restart();

					// TODO: check size of payload.  A little difficult here since it's based on the serialized
					// size, which isn't available at this point. And, we can't really make it available here
					// since we're passing the messages along to the Destination once we have a batch.
					//
					while (_batch.Count < _limits.Count && sw.Elapsed < _limits.Time && _queue.TryTake(out (Func<Packet[], Task> callback, PacketInfo[] messages) item, _limits.Time - sw.Elapsed)) {
						_batch.Add(item);
						c += item.messages.Length;
					}
                    if (c > 0)
                    {
                        try
                        {
                            await Console.Out.WriteLineAsync($"Sending {c} messages...");

                            var all = _batch.SelectMany(i => i.messages.Select(j => new { i.callback, message = j })).ToArray();
                            var callbacks = all
                                .GroupBy(m => new
                                {
                                    m.message.From.IpAddress,
                                    m.message.From.Port
                                })
                                .ToDictionary(g => g.Key, g => g.Select(m => m.callback).First());

                            await Console.Out.WriteLineAsync($"Batched for {callbacks.Count} callbacks...");

                            await Destination.AcceptMessages(async packets =>
                                    await Task.WhenAll(packets.GroupBy(p => new
                                    {
                                        IpAddress = p.Remote.Address.ToString(),
                                        p.Remote.Port
                                    }).Select(g => callbacks[g.Key](g.ToArray()))),
                                all.Select(i => i.message).ToArray());

                            Interlocked.Add(ref _count, c);
                            Interlocked.Add(ref _total, c);

                            _batch.Clear();
                        }
                        catch (Exception e)
                        {
                            await Console.Out.WriteLineAsync($"Lost a batch of {c} messages on error {e.Message}.");
                        }
                    }
                    else await Task.Delay(1);
				} catch (Exception e) {
					await Console.Out.WriteLineAsync($"Error building batch in thread {Thread.CurrentThread.ManagedThreadId} ({e.Message}).");
					await Task.Delay(TimeSpan.FromMilliseconds(100)); // TODO: rate-based slow down here.
				}
			}
			await Console.Out.WriteLineAsync($"Exiting task thread {Thread.CurrentThread.ManagedThreadId}");
		}

		public (TimeSpan timespan, int count) Rate {
			get {
				var now = DateTime.Now;
				var ts = now - _last;
				_last = now;
				return (ts, Interlocked.Exchange(ref _count, 0));
			}
		}

		public long Total { get { return _total; } }
		public int QueueLength { get { return _queue.Count; } }

		public IProxyDestination Destination { get; private set; }
		public BatchLimits Limits { get; private set; }

		public void MarkCompleted() { _queue.CompleteAdding(); }

		public async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			if (!_queue.IsAddingCompleted)
				_queue.Add((callback, messages), _cts.Token);
		}
	}
}
