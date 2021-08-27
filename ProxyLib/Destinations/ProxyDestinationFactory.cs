using Cppl.ProxyLib.Batching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Cppl.ProxyLib.Destinations
{
	public interface IDestinationManager
	{
		IProxyDestination GetDestination(IEnumerable<string> names);
	}
	public static class ProxyDestinationFactory
	{
		private class ProxyDestinationManager : IDestinationManager
		{
			public IList<CompositeDestination> CompositeDestinations { get; private set; } = new List<CompositeDestination>();

			public IDictionary<string, IProxyDestination> NamedDestinations;
			public IProxyDestination GetDestination(IEnumerable<string> names) {
				switch (names.Count()) {
					case 0:
						return null;
					case 1:
						return NamedDestinations[names.First()];
					default: {
						var d = new CompositeDestination(n => NamedDestinations[n]) { Names = names.ToArray() };
						CompositeDestinations.Add(d);
						return d;
					}
				}
			}
		}

		public static IDestinationManager SetupDestinations(IDictionary<string, ProxyConfig.Destination> config, CancellationTokenSource cts) {
			return new ProxyDestinationManager() {
				NamedDestinations = config.ToDictionary(kv => kv.Key, kv => SetupDestination(kv.Value, cts))
			};
		}

		private static IProxyDestination SetupDestination(ProxyConfig.Destination destination, CancellationTokenSource cts) {
			// TODO: states and firehose *require* a role to assume when cross-account access is
			// involved in using the delivery resource. So, here we'll need to add the RoleArn and
			// external ID to the destination. Note that it *doesn't* apply to composite destinations (above).
			IProxyDestination d = null;
			switch (destination.DeliveryArn.Service) {
				case "firehose":
					d = new FirehoseDestination(destination.DeliveryArn) { Role = destination.Role };
					break;
				case "states":
					d = new StatesDestination(destination.DeliveryArn) { Role = destination.Role };
					break;
				case "s3":
					d = new S3Destination(destination.DeliveryArn) { Role = destination.Role };
					break;
				case "lambda":
					d = new LambdaDestination(destination.DeliveryArn) { Role = destination.Role };
					break;
				case "sns":
					d = new SnsDestination(destination.DeliveryArn) { Role = destination.Role };
					break;
				case "sqs":
					d = new SqsDestination(destination.DeliveryArn) { Role = destination.Role };
					break;
				default:
					throw new NotImplementedException($"Unsupported delivery service in ARN") { Data = { { "Service", destination.DeliveryArn.Service } } };
			}
			return destination.Batching != null ? BatcherFactory.GetBatcher(destination.Batching, d, cts) : d;
		}
	}
}
