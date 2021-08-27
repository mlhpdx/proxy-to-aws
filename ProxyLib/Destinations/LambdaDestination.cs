using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cppl.ProxyLib.Json;

namespace Cppl.ProxyLib.Destinations
{
	internal class LambdaDestination : BaseDestination<AmazonLambdaClient>
	{
		internal class LambdaArnFunctionAndQualifier
		{
			private static Regex _parser = new Regex(@"^function:(?<FunctionName>[^:\s]+)(?::(?<Qualifier>.+))?\s*$", RegexOptions.Compiled);
			public LambdaArnFunctionAndQualifier(Arn arn) {
				Arn = arn;
				var match = _parser.Match(arn.Resource);
				if (!match.Success)
					throw new ArgumentException($"ARN resource doesn't appear to be a Lambda function name/qualifier", "arn") { Data = { { "Resource", arn.Resource } } };
				FunctionName = match.Groups["FunctionName"].Value;
				Qualifier = match.Groups["Qualifier"]?.Value?.Trim();
				if (string.IsNullOrEmpty(Qualifier))
					Qualifier = "$LATEST";
			}
			public Arn Arn { get; private set; }
			public string FunctionName { get; private set; }
			public string Qualifier { get; private set; }
		}

		LambdaArnFunctionAndQualifier _functionname;

		internal LambdaDestination(Arn arn) {
			_functionname = new LambdaArnFunctionAndQualifier(arn);
			
			// TODO: need to figure out how/where to override the defaults for destination
			// calls. Maybe an OnClientCreated event or override?
			//
			//_lambda = new AmazonLambdaClient(new AmazonLambdaConfig() {
			//	RegionEndpoint = RegionEndpoint.USWest2,
			//	Timeout = TimeSpan.FromSeconds(2),
			//	MaxErrorRetry = 2,
			//	ReadWriteTimeout = TimeSpan.FromSeconds(2)
			//});
		}

		public override string AWSRegion => _functionname.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Invoking Lambda {_functionname.FunctionName}");

			InvokeResponse response = null;
			try {
				response = await Client.InvokeAsync(new InvokeRequest() {
					FunctionName = _functionname.FunctionName,
					Qualifier = _functionname.Qualifier,
					InvocationType = InvocationType.RequestResponse,
					LogType = LogType.None,
					Payload = $"[{string.Join(",", messages.Select(m => m.ToString()))}]"
				});
			} catch (Exception e) {
				await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Exception calling Lambda: {e.ToString()}");
			}

			await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Reading Lambda response...");

			var text = string.Empty;
			using (var sr = new StreamReader(response.Payload))
				text = await sr.ReadToEndAsync();

			// await Console.Out.WriteLineAsync($"{DateTime.UtcNow}: Lambda response is {text}");

			var replies = JsonConvert.DeserializeAnonymousType(text, new[] {
				new {
					Local = new IPEndPoint(IPAddress.Any, 0),
					Remote = new IPEndPoint(IPAddress.Any, 0),
					Base64Reply = string.Empty
				}
			}, new JsonSerializerSettings() { Converters = new List<JsonConverter>() { new IPAddressConverter(), new IPEndPointConverter() } });

			await callback(replies.Select(r => new Packet() {
				Local = r.Local,
				Remote = r.Remote,
				Data = Convert.FromBase64String(r.Base64Reply)
			}).ToArray());
		}
	}
}
