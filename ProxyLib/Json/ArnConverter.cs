﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Cppl.ProxyLib.Json
{
	public class ArnConverter : JsonConverter
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public override bool CanConvert(Type objectType) {
			if (objectType == typeof(Arn))
				return true;
			if (objectType == typeof(List<Arn>))
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
			// convert an ipaddress represented as a string into an IPAddress object and return it to the caller
			if (objectType == typeof(Arn)) {
				return new Arn(JToken.Load(reader).ToString());
			}

			// convert an array of IPAddresses represented as strings into a List<IPAddress> object and return it to the caller
			if (objectType == typeof(List<IPAddress>)) {
				return JToken.Load(reader).Select(address => new Arn((string)address)).ToList();
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
			// convert an IPAddress object to a string representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(Arn)) {
				JToken.FromObject(value.ToString()).WriteTo(writer);
				return;
			}

			// convert a List<IPAddress> object to a string[] representation of itself and Write it to the serialiser
			if (value.GetType() == typeof(List<Arn>)) {
				JToken.FromObject((from n in (List<Arn>)value select n.ToString()).ToList()).WriteTo(writer);
				return;
			}

			throw new NotImplementedException();
		}
	}
}
