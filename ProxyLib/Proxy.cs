using Cppl.ProxyLib.Destinations;
using Cppl.ProxyLib.Listeners;
using ProxyLib.Admin.Monitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cppl.ProxyLib
{
	public class Proxy
	{
		MonitorAgent _monitor;
		IDestinationManager _destinations;
		ICollection<IListener> _listeners;

		public static Task Run(ProxyConfig config, CancellationTokenSource cts = null) {

			var proxy = new Proxy();

			// TODO: HACK. The monitor agent should be an instance variable of the proxy, not a
			// global static.  But I'm not solving the issue of connecting the listener to the 
			// agent at this point. Ugh.
			MonitorAgent.MonitorTopicArn = config.Admin.Monitor;
			MonitorAgent.Cts = cts;

			proxy._destinations = ProxyDestinationFactory.SetupDestinations(config.Destinations, cts);
			proxy._listeners = config.Bindings
				.Select(b => (b, r: ClientNetworkRestrictionFactory.Get(b.Clients), d: proxy._destinations.GetDestination(b.Destination)))
				.SelectMany(t => ListenerFactory.AttachListeners(t.b).WithDestination(t.d).WithClientNetworks(t.r))
				.ToList();

			var tasks = proxy._listeners.Select(l => l.Listen(cts)).ToArray();

			// monitor the listeners for early exit
			var task = Task.Run(() => {
				while (tasks.Any()) {
					var completed = tasks[Task.WaitAny(tasks)];
					var outcome = completed.IsCompleted && !completed.IsFaulted ? "Success" : completed.Exception?.ToString() ?? "Unknown";
					Console.Out.WriteLine($"Listener {completed.Id} exited: {outcome}");

					// TODO: Attempt to restart the listener?
					tasks = tasks.Except(new[] { completed }).Where(t => t.Status != TaskStatus.WaitingForActivation).ToArray();
				}
			});

			Task.Delay(100).GetAwaiter().GetResult();

			return task;
			//await task;
			//while (cts?.IsCancellationRequested != true)
			//	Task.Delay(1000);
		}
	}
}
