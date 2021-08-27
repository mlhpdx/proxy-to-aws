using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class FirehoseDestination : BaseDestination<AmazonKinesisFirehoseClient>
	{
		internal class FirehoseArnDeliveryStream
		{
			private static Regex _parser = new Regex(@"^deliverystream/(?<DeliveryStreamName>.+)\s*$", RegexOptions.Compiled);
			public FirehoseArnDeliveryStream(Arn arn) {
				Arn = arn;
				var match = _parser.Match(arn.Resource);
				if (!match.Success)
					throw new ArgumentException($"ARN resource doesn't appear to be a Firehose deliver stream name", "arn") { Data = { { "Resource", arn.Resource } } };
				DeliveryStreamName = match.Groups["DeliveryStreamName"].Value.Trim();
			}
			public Arn Arn { get; private set; }
			public string DeliveryStreamName { get; private set; }
		}

		FirehoseArnDeliveryStream _deliverystream;

		internal FirehoseDestination(Arn arn, Credentials credentials = null) {
			_deliverystream = new FirehoseArnDeliveryStream(arn);
		}
		public override string AWSRegion => _deliverystream.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			var b = new StringBuilder();
			b.Append(string.Join("\n", messages.Select(m => m.ToString())));

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(b.ToString()))) {
				var record = new Record() { Data = ms };
				await Client.PutRecordAsync(_deliverystream.DeliveryStreamName, record);
			}
		}
	}
}
