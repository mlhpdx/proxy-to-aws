using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cppl.ProxyLib.Json
{
	public class PromotionToArrayConverter<ITEM_TYPE> : JsonConverter
	{
		static MethodInfo _parse = typeof(ITEM_TYPE).GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == "Parse" && m.ReturnType == typeof(ITEM_TYPE)
				&& m.GetParameters().Count() == 1
				&& m.GetParameters().FirstOrDefault()?.ParameterType == typeof(string));
		static ITEM_TYPE TokenToObject(JToken token) {
			return _parse != null ? (ITEM_TYPE)_parse.Invoke(null, new object[] { token.ToString() }) : token.ToObject<ITEM_TYPE>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public override bool CanConvert(Type objectType) {
			if (objectType == typeof(List<ITEM_TYPE>))
				return true;
			if (objectType == typeof(ITEM_TYPE[]))
				return true;
			if (objectType == typeof(IEnumerable<ITEM_TYPE>))
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
			// convert json to an array of items, promoting a single value to an array if needed, and return it to the caller.
			var token = JToken.Load(reader);
			var items = token.Type == JTokenType.Array ? token.Select(value => TokenToObject(value))
				: new[] { TokenToObject(token) };
			if (objectType == typeof(List<ITEM_TYPE>)) {
				return items.ToList();
			} else if (objectType == typeof(ITEM_TYPE[])) {
				return items.ToArray();
			} else if (objectType == typeof(IEnumerable<ITEM_TYPE>)) {
				return items;
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
			// convert a List<PortRange> object to a string[] representation of itself and Write it to the serialiser
			if (typeof(IEnumerable<ITEM_TYPE>).IsAssignableFrom(value.GetType())) {
				var items = (IEnumerable<ITEM_TYPE>)value;
				switch (items.Count()) {
					default: // more than one
						JToken.FromObject(items.Select(v => JToken.FromObject(v))).WriteTo(writer);
						break;
					case 1:
						JToken.FromObject(items.First()).WriteTo(writer);
						break;
					case 0:
						break;
				}
				return;
			}

			throw new NotImplementedException();
		}
	}
}
