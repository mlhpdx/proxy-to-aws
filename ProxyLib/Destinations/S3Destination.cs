using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cppl.ProxyLib.Destinations
{
	internal class S3Destination : BaseDestination<AmazonS3Client>
	{
		internal class S3ArnObjectKeyPrefix
		{
			private static Regex _parser = new Regex(@"^(?<BucketName>[^/]+)/(?<KeyPrefix>.+)\s*$", RegexOptions.Compiled);
			public S3ArnObjectKeyPrefix(Arn arn) {
				Arn = arn;
				var match = _parser.Match(arn.Resource);
				if (!match.Success)
					throw new ArgumentException($"ARN resource doesn't appear to be an s3 object prefix", "arn") { Data = { { "Resource", arn.Resource } } };
				BucketName = match.Groups["BucketName"].Value.Trim();
				KeyPrefix = match.Groups["KeyPrefix"].Value.Trim('/').Trim();
			}
			public Arn Arn { get; private set; }
			public string BucketName { get; private set; }
			public string KeyPrefix { get; private set; }
		}

		S3ArnObjectKeyPrefix _prefix;

		internal S3Destination(Arn arn) {
			_prefix = new S3ArnObjectKeyPrefix(arn);
		}

		public override string AWSRegion => _prefix.Arn.Region;

		public override async Task AcceptMessages(Func<Packet[], Task> callback, params PacketInfo[] messages) {
			var b = new StringBuilder();
			b.Append(string.Join("\n", messages.Select(m => m.ToString())));

			var now = DateTime.UtcNow;
			var name = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-");
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(b.ToString()))) {
				var record = new PutObjectRequest() {
					ContentBody = b.ToString(),
					BucketName = _prefix.BucketName,
					Key = $"{_prefix.KeyPrefix}/{now.Year:0000}/{now.Month:00}/{now.Day:00}/{now.Hour:00}/{name}.ndjson"
				};
				await Client.PutObjectAsync(record);
			}
		}
	}
}
