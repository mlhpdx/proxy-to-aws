using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal abstract class BaseDestination<T> : IProxyDestination, IDisposable where T : AmazonServiceClient
	{
		T _client;
		protected T Client { get => _client ?? (_client = GetClient()); }

		ProxyConfig.ExternalRoleInfo _role;
		public ProxyConfig.ExternalRoleInfo Role { get => _role; set => _role = value; }

		internal BaseDestination(ProxyConfig.ExternalRoleInfo role = null) {
			_role = role;
		}

		public abstract string AWSRegion { get; }

		T GetClient() {
			T c = null;
			var region = RegionEndpoint.GetBySystemName(AWSRegion ?? AWSConfigs.AWSRegion);
			if (_role == null)
				c = (T)Activator.CreateInstance(typeof(T), new object[] { region });
			else {
				// TODO: firehose doesn't support resource policies, so we must assume a role in order
				// to deliver to another account. So, we need to check the account we're in and the one
				// that the resource is in, and if they're different look for the Role: { Arn: "arn:aws:iam...",  ExternalId: "..." } to 
				// determine which role to assume and the external id to use when doing so.  This same behavior is
				// *optional* for sns, sqs, lambda and S3 but also *required* for step functions.
				//
				var sts = new AmazonSecurityTokenServiceClient();
				var identity = sts.AssumeRoleAsync(new AssumeRoleRequest() {
					RoleArn = _role.RoleArn.ToString(),
					ExternalId = _role.ExternalId,
					RoleSessionName = "PAWS-Proxy@" + Environment.MachineName,
					DurationSeconds = (int?)_role.Duration?.TotalSeconds ?? 3600 // default -- can be up to 12 hours if allowed
				}).Result;
				var args = new object[] {
					identity.Credentials.AccessKeyId,
					identity.Credentials.SecretAccessKey,
					identity.Credentials.SessionToken,
					region
				};
				c = (T)Activator.CreateInstance(typeof(T), args);
			}
			return c;
		}

		// TODO: Is this the right place to record usage (packets in/out) for billing? Is there a better place/way?
		abstract public Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages);

		public void Dispose() {
			_client.Dispose();
			_client = null;
		}
	}
}
