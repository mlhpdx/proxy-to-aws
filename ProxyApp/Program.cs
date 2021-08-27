using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Util;
using Cppl.ProxyApp.UI;
using Cppl.ProxyLib;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cppl.ProxyApp
{
	class Program
	{
		public static async Task<int> Main(string[] args) {
			var cts = new CancellationTokenSource();
			Console.CancelKeyPress += (s, e) => cts.Cancel();

			var config = await ParseConfig(args);
			Application app = null;

			if (!config.Headless) {
				app = new Application() {
					OnSuspending = () => { }, // TODO: suspend listeners
					OnResuming = () => { },  // TODO: resume listeners
					OnExit = () => cts.Cancel(false)
				};
			}

			ServicePointManager.DefaultConnectionLimit = 1000;

			var ips = Dns.GetHostAddresses(Dns.GetHostName());
			var ip = ips.Where(i => i.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault() ?? ips[0];
			await Console.Out.WriteLineAsync($"My IP address seems to be: {ip?.ToString() ?? "Unknown"}");

			Task task = Proxy.Run(config, cts);

			if (app != null)
				task = app.Start();

			while (!cts.IsCancellationRequested) {
				await Task.Delay(1000);
			}

			return 0;
		}

		private static async Task<ProxyConfig> ParseConfig(string[] args) {
			var path = args.Length > 0 ? args[0] : System.Environment.GetEnvironmentVariable("PAWS_CONFIG_PATH") ?? "ProxyConfig.json";
			await Console.Out.WriteLineAsync($"Using config path: {path}");
			try {
				if (File.Exists(path)) 
					return JsonConvert.DeserializeObject<ProxyConfig>(await File.ReadAllTextAsync(path));
				
				if (S3Uri.IsS3Uri(new Uri(path))) {
					var s3uri = new AmazonS3Uri(path);
					var s3 = new AmazonS3Client(s3uri.Region);
					var outcome = await s3.GetObjectAsync(s3uri.Bucket, s3uri.Key);
					return JsonConvert.DeserializeObject<ProxyConfig>(await new StreamReader(outcome.ResponseStream).ReadToEndAsync());
				}

				throw new ArgumentException("Configuration file name or S3 ARN must be provided", "args");
			} catch (Exception ex) {
				Console.WriteLine($"{DateTime.UtcNow}: Bad command-line ({ex.Message}):\n  Usage: UdpProxy <config-file>");
				System.Environment.Exit(-1);
				throw; // never gets here
			}
		}
	}
}
