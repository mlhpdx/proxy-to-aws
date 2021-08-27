using Amazon;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class StatesDestination : BaseDestination<AmazonStepFunctionsClient>
	{
		internal class StatesArnStateMachineName
		{
			private static Regex _parser = new Regex(@"^stateMachine:(?<StateMachineName>[^:\s]+)\s*$", RegexOptions.Compiled);
			public StatesArnStateMachineName(Arn arn) {
				Arn = arn;
				var match = _parser.Match(arn.Resource);
				if (!match.Success)
					throw new ArgumentException($"ARN resource doesn't appear to be a StateMachine name", "arn") { Data = { { "Resource", arn.Resource } } };
				TopicName = match.Groups["TopicName"].Value.Trim();
			}
			public Arn Arn { get; private set; }
			public string TopicName { get; private set; }
		}

		StatesArnStateMachineName _stateMachineName;

		internal StatesDestination(Arn arn) {
			_stateMachineName = new StatesArnStateMachineName(arn);
		}

		public override string AWSRegion => _stateMachineName.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			var tasks = Client.StartExecutionAsync(new StartExecutionRequest() {
				StateMachineArn = _stateMachineName.Arn.ToString(),
				Input = $"{{ Messages: [ {string.Join(",", messages.Select(m => m.ToString()))} ] }}"
			});
			var responses = await Task.WhenAll(tasks);
			if (responses.Any(r => r.ExecutionArn == null))
				throw new ApplicationException($"Failed to proxy message to StateMachine.") { Data = { { "Arn", _stateMachineName.Arn } } };
		}
	}
}
