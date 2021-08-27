using System;
using System.Text.RegularExpressions;

namespace Cppl.ProxyLib
{
	public class Arn
	{
		private static Regex _parser = new Regex(@"^arn:(?<Partition>[^:]+):(?<Service>[^:]+):(?<Region>[^:]*):(?<AccountId>[^:]*):(?<Resource>.+)\s*$", RegexOptions.Compiled);

		public Arn(string arn) {
			var match = _parser.Match(arn);
			if (!match.Success)
				throw new ArgumentException($"input string does not appear to be an AWS ARN", "arn") { Data = { { "Input", arn } } };
			Partition = match.Groups["Partition"].Value;
			Service = match.Groups["Service"].Value;
			Region = match.Groups["Region"].Value;
			AccountId = match.Groups["AccountId"].Value;
			Resource = match.Groups["Resource"].Value.Trim();
		}

		public string Partition { get; private set; }
		public string Service { get; private set; }
		public string Region { get; private set; }
		public string AccountId { get; private set; }
		public string Resource { get; private set; }

		public static implicit operator Arn(string arn) { return new Arn(arn); }

		public override string ToString() {
			return $"arn:{Partition}:{Service}:{Region}:{AccountId}:{Resource}";
		}
	}
}
