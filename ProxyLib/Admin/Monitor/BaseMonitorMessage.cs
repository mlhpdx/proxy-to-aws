using System;
using System.Collections.Generic;
using System.Text;

namespace ProxyLib.Admin.Monitor
{
	interface IMonitorMessage
	{
		string MessageType { get; }
		DateTime When { get; }
		string MachineName { get; }

	}

	abstract class BaseMonitorMessage<T> : IMonitorMessage
	{
		public string MessageType { get; } = typeof(T).Name;
		public DateTime When { get; set; } = DateTime.UtcNow;
		public string MachineName { get; set; } = Environment.MachineName;
	}
}
