using Amazon;
using Amazon.SimpleNotificationService;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class SnsDestination : BaseDestination<AmazonSimpleNotificationServiceClient>
	{
		internal class SnsArnTopicName
		{
			private static Regex _parser = new Regex(@"^(?<TopicName>[^\:\s]+)(?:\:[^\:\s/]+)?\s*$", RegexOptions.Compiled);
			public SnsArnTopicName(Arn arn) {
				Arn = arn;
				var match = _parser.Match(arn.Resource);
				if (!match.Success)
					throw new ArgumentException($"ARN resource doesn't appear to be an SNS topic name", "arn") { Data = { { "Resource", arn.Resource } } };
				TopicName = match.Groups["TopicName"].Value.Trim();
			}
			public Arn Arn { get; private set; }
			public string TopicName { get; private set; }
		}

		SnsArnTopicName _topicname;

		internal SnsDestination(Arn arn) {
			_topicname = new SnsArnTopicName(arn);
		}

		public override string AWSRegion => _topicname.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			var tasks = messages.Select(message => Client.PublishAsync(_topicname.Arn.ToString(), message.ToString()));
			var responses = await Task.WhenAll(tasks);
			if (responses.Any(r => r.MessageId == null))
				throw new ApplicationException($"Failed to proxy message to SNS.") { Data = { { "Arn", _topicname.Arn } } };
		}
	}
}
