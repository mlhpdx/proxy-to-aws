using Cppl.ProxyLib.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Cppl.ProxyLib
{
	public abstract class WithChangeNotifications : INotifyPropertyChanging, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;
		protected void OnPropertyChanged([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		protected void OnPropertyChanging([CallerMemberName] string prop = null) => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(prop));

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string prop = null) {
			if (EqualityComparer<T>.Default.Equals(field, value))
				return false;

			OnPropertyChanging(prop);
			field = value;
			OnPropertyChanged(prop);
			return true;
		}
	}
	public class ProxyConfig : WithChangeNotifications
	{
		public class AdminConfig : WithChangeNotifications
		{
			Arn _control;
			public Arn Control {
				get => _control;
				set => SetField(ref _control, value);
			}
			Arn _monitor;
			public Arn Monitor {
				get => _monitor;
				set => SetField(ref _monitor, value);
			}

		}
		public class PortRange : WithChangeNotifications
		{
			public PortRange() { }

			[JsonConstructor()]
			public PortRange(string spec) {
				var parts = spec.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Select(s => (int?)int.Parse(s));
				_first = parts.First().Value;
				_last = parts.Skip(1).FirstOrDefault() ?? First;
			}
			[JsonIgnore]
			public IEnumerable<int> Values { get { return Enumerable.Range(_first, _last - _first+ 1); } }
			int _first;
			public int First {
				get => _first;
				set => SetField(ref _first, value);
			}
			int _last;
			public int Last {
				get => _last;
				set => SetField(ref _last, value);
			}
		}

		public class ExternalRoleInfo : WithChangeNotifications
		{
			Arn _roleArn;
			public Arn RoleArn {
				get => _roleArn;
				set => SetField(ref _roleArn, value);
			}
			string _externalId;
			public string ExternalId {
				get => _externalId;
				set => SetField(ref _externalId, value);
			}
			TimeSpan? _duration;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public TimeSpan? Duration {
				get => _duration;
				set => SetField(ref _duration, value);
			}
		}

		public class ClientNetworkRestriction : WithChangeNotifications
		{
			/// <summary>
			/// List of CIDRs to use to verify that a client ip should be allowed to connect.
			/// </summary>
			IEnumerable<IPNetwork> _allowedNetworks;
			[JsonConverter(typeof(PromotionToArrayConverter<IPNetwork>))]
			public IEnumerable<IPNetwork> AllowedNetworks {
				get => _allowedNetworks;
				set => SetField(ref _allowedNetworks, value);
			}

			/// <summary>
			///	List of domain names to use to verify (via DNS lookup of A records) that a client
			///	ip should be allowed to connect.  The client IP must match at least one of the
			///	A records associated with the domain.
			/// </summary>
			IEnumerable<string> _allowedDomains;
			[JsonConverter(typeof(PromotionToArrayConverter<string>))]
			public IEnumerable<string> AllowedDomains {
				get => _allowedDomains;
				set => SetField(ref _allowedDomains, value);
			}

			/// <summary>
			/// List of subject names for certificates that can be used to authorized client connections.  Leave empty to allow
			/// any client certificate to be used. Prefix subject name with '~' to disable IsValid() checks on the cert,
			/// or with '!' to require the hash of the certificate provided by the client to exactly match that of the 
			/// certificate with the same subject name in the local user store.
			/// </summary>
			IEnumerable<string> _tlsClientCertificates;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			[JsonConverter(typeof(PromotionToArrayConverter<string>))]
			public IEnumerable<string> TlsClientCertificates {
				get => _tlsClientCertificates;
				set => SetField(ref _tlsClientCertificates, value);
			}
		}

		public class Binding : WithChangeNotifications
		{
			public enum Protocol {[Description("udp")] UDP, [Description("tcp")] TCP, [Description("tls")] TLS, [Description("icmp")] ICMP }

			/// <summary>
			/// The local interfaces on which to listen for inbound UDP messages.  The default is to listen on all
			/// interfaces (IPAddress.Any).
			/// </summary>
			IPAddress _ipAddress;
			[JsonConverter(typeof(IPAddressConverter))]
			public IPAddress IPAddress {
				get => _ipAddress;
				set => SetField(ref _ipAddress, value);
			}

			/// <summary>
			/// 
			/// </summary>
			ClientNetworkRestriction _clients;
			public ClientNetworkRestriction Clients {
				get => _clients;
				set => SetField(ref _clients, value);
			}

			/// <summary>
			/// The local ports on which to listen for the given IP address.
			/// </summary>
			PortRange _ports;
			[JsonConverter(typeof(PortRangeConverter))]
			public PortRange Ports {
				get => _ports;
				set => SetField(ref _ports, value);
			}

			/// <summary>
			///  The protocol(s) to implement for this binding ("udp", "tcp", "tls"). Value can be provided
			///  in the JSON config as either a single value, or an array of values.
			/// </summary>
			Protocol[] _protocols;
			[JsonConverter(typeof(PromotionToArrayConverter<Protocol>))]
			public IEnumerable<Protocol> Protocols {
				get => _protocols;
				set => SetField(ref _protocols, value.ToArray());
			}

			/// <summary>
			/// 
			/// </summary>
			string _streamSplitter;
			public string StreamSplitter {
				get => _streamSplitter;
				set => SetField(ref _streamSplitter, value);
			}

			/// <summary>
			/// An name of the object in the Destinations dictionary that specify the configuration 
			/// of the destination service resource for this binding. 
			/// </summary>
			string[] _destination;
			[JsonConverter(typeof(PromotionToArrayConverter<string>))]
			public IEnumerable<string> Destination {
				get => _destination;
				set => SetField(ref _destination, value.ToArray());
			}

			/// <summary>
			///	The subject name of a certificate in the user store for use as the server certificate 
			///	for a TLS protocol binding. *Required* for TLS.
			/// </summary>
			public string _tlsServerCertificate;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public string TlsServerCertificate {
				get => _tlsServerCertificate;
				set => SetField(ref _tlsServerCertificate, value);
			}
		}

		/// <summary>
		/// Configuration that controls the size of payloads and frequency at which they are delivered to a
		/// destination.  At least one of Count, Size or Timeout should be specified.
		/// </summary>
		public class Batching : WithChangeNotifications
		{
			/// <summary>
			/// The maximum number of items to include in a batch. Optional.
			/// </summary>
			private int? _count;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public int? Count { get => _count; set => SetField(ref _count, value); }

			/// <summary>
			/// The maximum size of a batch in terms of total bytes.  Not yet implmenented, and perhaps problematic
			/// in concept -- presumably this is the limit of the outgoing payload, not the incomming bytes? Or?
			/// Optional.
			/// </summary>
			private float? _size;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public float? SizeInMB { get => _size; set => SetField(ref _size, value); }

			/// <summary>
			/// The maximum time in seconds (decimal) to wait before sending out a batch that is partially filled (per
			/// the other batching rules on count and size). No message should linger in a batch for longer than this
			/// before the delivery process starts (still may take longer to get to the destination). Optional.
			/// </summary>
			private float? _timeout;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public float? TimeoutInSeconds { get => _timeout; set => SetField(ref _timeout, value); }
		}

		public class Destination : WithChangeNotifications
		{
			/// <summary>
			/// Where received messages will be sent. May be the ARN of a Lambda function, S3 bucket, SNS topic, 
			/// SQS queue, Step Functions state machine, Kinesis Firehose (or when I figure it out an HTTPS URL).
			/// The ARN may identify a resource in a different region from where PAWS is running, but there will be
			/// obvious latency and reliability reductions in such a setup.
			/// </summary>
			private Arn _deliveryArn;
			public Arn DeliveryArn { get => _deliveryArn; set => SetField(ref _deliveryArn, value); }

			/// <summary>
			/// If delivery to the resource requires assuming a role (as it should, unless the EC2 instance profile's
			/// role is granted permission) then it can be specified here.
			/// </summary>
			private ExternalRoleInfo _externalRole;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public ExternalRoleInfo Role { get => _externalRole; set => SetField(ref _externalRole, value); }

			/// <summary>
			/// Batching configuration for this destination.
			/// </summary>
			private Batching _batching;
			[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
			public Batching Batching { get => _batching; set => SetField(ref _batching, value); }
		}

		/// <summary>
		/// Configures the comminucation channels for control and monitoring.
		/// </summary>
		private AdminConfig _admin;
		public AdminConfig Admin { get => _admin; set => SetField(ref _admin, value); }

		/// <summary>
		/// Names and configuration of destinations that will receive forwarded network traffic from listeners.
		/// </summary>
		private Dictionary<string, Destination> _destinations;
		public Dictionary<string, Destination> Destinations { get => _destinations; set => SetField(ref _destinations, value); }

		/// <summary>
		/// Collection of configuraiton of listeners which determine the ports, protocols and other settings
		/// that will result in accepting traffic to be forwarded to destinations.
		/// </summary>
		private Binding[] _bindings;
		public Binding[] Bindings { get => _bindings; set => SetField(ref _bindings, value); }

		/// <summary>
		/// Controls presentation (or not) of the terminal UI.
		/// </summary>
		private bool _headless;
		public bool Headless { get => _headless; set => SetField(ref _headless, value); }
	}
}
