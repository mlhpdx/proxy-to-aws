using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Cppl.ProxyLib.Batching;

namespace Cppl.ProxyApp.UI
{
	internal class Application
	{
		public class ConsoleWriter : TextWriter
		{
			public override Encoding Encoding { get { return Encoding.UTF8; } }

			string _buffer;
			public override void Write(string value) {
				_buffer += value;
			}

			public override void WriteLine(string value) {
				OnWriteLine?.Invoke(_buffer + value);
				_buffer = string.Empty;
			}

			public Action<string> OnWriteLine = s => { };
		}

		public Application() {
			var writer = new ConsoleWriter() {
				OnWriteLine = s => Messages?.Add((DateTime.Now, s))
			};

			Console.SetError(writer);
			Console.SetOut(writer);
		}

		public Action OnSuspending;
		public Action OnResuming;

		public Action OnExit;

		public IList<(DateTime time, string message)> Messages = new List<(DateTime time, string message)>();

		static readonly IList<char> _verticals = new char[] { ' ', '\x2581', '\x2582', '\x2583', '\x2584', '\x2585', '\x2586', '\x2587' }.ToList().AsReadOnly();
		static readonly IList<char> _horizontals = new char[] { ' ', '\x258F', '\x258E', '\x258D', '\x258C', '\x258B', '\x258A', '\x2589' }.ToList().AsReadOnly();

		static readonly char _whole = '\x2588';

		volatile static bool _suspended = false;

		public async Task Start() {
			Terminal.Gui.Application.Init();

			var top = Terminal.Gui.Application.Top;
			var win = new Window("UDP Proxy to AWS");
			top.Add(win);

			var bk = new Label("-") { X = 10, Y = 1, Width = 8 };
			var ql = new Label("-") { X = 10, Y = 2, Width = 8 };
			win.Add(new Label("Backlog:") { X = 1, Y = 1 }, ql, bk);

			var aq = new Label("-") { X = 30, Y = 1, Width = 8 };
			var rate = new Label("-") { X = 30, Y = 2, Width = 8 };
			win.Add(new Label("Activity:") { X = 20, Y = 1 }, rate, aq);

			var quit = new Button("Quit") { X = 60, Y = 1 };
			quit.Clicked += () => Terminal.Gui.Application.RequestStop();
			
			var noop = new Button("No-op ") { X = 70, Y = 1, Width = 10 };
			noop.Clicked += () => {
				if (_suspended) {
					OnResuming?.Invoke();
					_suspended = false;
					noop.Text = "No-op ";
				} else {
					OnSuspending?.Invoke();
					_suspended = true;
					noop.Text = "Resume";
				}
			};
			win.Add(quit, noop);

			var chart = new SideScroller() { X = 1, Y = 3, Width = Dim.Fill() - 1, Height = 10 };
			win.Add(chart);

			var log = new TextView() {
				X = 1,
				Y = 15,
				Width = Dim.Fill() - 1,
				Height = Dim.Fill(),
				ReadOnly = true
			};
			win.Add(log);

			Terminal.Gui.Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(1.0), m => {
				var rv = BatcherFactory.InstantaniousTotalRate; // forces snapshot

				ql.Text = $"{BatcherFactory.TotalQueueLength}";
				bk.Text = $"{BatcherFactory.BackloggedQueueCount} of {BatcherFactory.QueueCount}";

				aq.Text = $"{BatcherFactory.ActiveQueueCount} of {BatcherFactory.QueueCount}";
				rate.Text = $"{rv:0.0} r/s";

				var scale = 4.0;
				rv = rv < 1.0 ? rv : Math.Log(rv, 2) * scale;
				var w = (int)((rv) / _verticals.Count);
				var p = (int)((rv) % _verticals.Count);
				var col = Enumerable.Repeat(_whole, w).Concat(new[] { _verticals[p] });

				chart.PushColumn(col.Select(c => new System.Rune(c)));

				if (Messages?.Any() == true) {
					log.Text = string.Join("\n", Messages.OrderByDescending(_ => _.time)
						.Select(l => $"{l.time}: {l.message}")) + log.Text;
					Messages.Clear();
				}
				return true;
			});

			Terminal.Gui.Application.Run();

			OnExit?.Invoke();
		}
	}
}
