using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Cppl.ProxyLib.Json
{
	class IPEndPointConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) {
			if (objectType == typeof(IPEndPoint))
				return true;
			if (objectType == typeof(List<IPEndPoint>))
				return true;

			return false;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (objectType == typeof(IPEndPoint)) {
				var t = JToken.Load(reader);
				return new IPEndPoint(IPAddress.Parse(t["IpAddress"].ToString()), t["Port"].Value<int>());
			}

			// convert an array of IPAddresses represented as strings into a List<IPAddress> object and return it to the caller
			if (objectType == typeof(List<IPEndPoint>)) {
				return JToken.Load(reader).Select(t => new IPEndPoint(IPAddress.Parse(t["IpAddress"].ToString()), t["Port"].Value<int>())).ToList();
			}

			throw new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			// convert an IPAddress object to a string representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(IPEndPoint)) {
				var ep = value as IPEndPoint;
				var j = new JObject(
					new JProperty("IpAddress", ep.Address.ToString()),
					new JProperty("Port", ep.Port)
				);
				j.WriteTo(writer);
				return;
			}

			// convert a List<IPAddress> object to a string[] representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(List<IPAddress>)) {
				var l = value as List<IPEndPoint>;
				var a = new JArray(l.Select(ep => new JObject(
					new JProperty("IpAddress", ep.Address.ToString()),
					new JProperty("Port", ep.Port)
				)));
				a.WriteTo(writer);
				return;
			}

			throw new NotImplementedException();
		}
	}
}
