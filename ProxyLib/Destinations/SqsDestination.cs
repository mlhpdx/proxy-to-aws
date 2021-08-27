using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class SqsDestination : BaseDestination<AmazonSQSClient>
	{
		internal class SqsArnQueueName
		{
			private static Regex _parser = new Regex(@"^(?<TopicName>[^\:\s]+)(?:\:[^\:\s/]+)?\s*$", RegexOptions.Compiled);
			public SqsArnQueueName(Arn arn) {
				Arn = arn;
			}
			public Arn Arn { get; private set; }
			public string QueueName { get => Arn.Resource; }
		}

		SqsArnQueueName _queuename;
		string _queueurl;

		internal SqsDestination(Arn arn) {
			_queuename = new SqsArnQueueName(arn);
		}

		public override string AWSRegion => _queuename.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			var url = _queueurl ?? (_queueurl = (await Client.GetQueueUrlAsync(_queuename.QueueName)).QueueUrl);
			var entries = messages.Select(m => new SendMessageBatchRequestEntry() {
				Id = Guid.NewGuid().ToString(),
				MessageBody = m.ToString()
			}).ToList();
			await Client.SendMessageBatchAsync(_queueurl, entries);
		}
	}
}
