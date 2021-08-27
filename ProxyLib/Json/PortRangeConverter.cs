using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using static Cppl.ProxyLib.ProxyConfig;

namespace Cppl.ProxyLib.Json
{
	public class PortRangeConverter : JsonConverter
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public override bool CanConvert(Type objectType) {
			if (objectType == typeof(PortRange))
				return true;
			if (objectType == typeof(List<PortRange>))
				return true;

			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			// convert an port range represented as a string into an PortRange object and return it to the caller
			if (objectType == typeof(PortRange)) {
				return new PortRange(JToken.Load(reader).ToString());
			}

			// convert an array of port ranges represented as strings into a List<PortRange> object and return it to the caller
			if (objectType == typeof(List<PortRange>)) {
				return JToken.Load(reader).Select(address => new PortRange((string)address)).ToList();
			}

			throw new NotImplementedException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			// convert an PortRange object to a string representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(PortRange)) {
				JToken.FromObject(value.ToString()).WriteTo(writer);
				return;
			}

			// convert a List<PortRange> object to a string[] representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(List<PortRange>)) {
				JToken.FromObject((from n in (List<PortRange>)value select n.ToString()).ToList()).WriteTo(writer);
				return;
			}

			throw new NotImplementedException();
		}
	}
}
