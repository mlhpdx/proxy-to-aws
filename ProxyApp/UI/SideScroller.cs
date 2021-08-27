using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace Cppl.ProxyApp.UI
{
	class SideScroller : View
	{
		public SideScroller() { }
		public SideScroller(Rect frame) : base(frame) { }

		List<Rune[]> _columns = new List<Rune[]>();

		public void PushColumn(IEnumerable<Rune> values) {
			var v = values.Concat(Enumerable.Repeat((Rune)' ', Frame.Height)).Take(Frame.Height).ToArray();

			_columns.Insert(0, v);
			if (_columns.Count > Frame.Width)
				_columns.RemoveRange(Frame.Width, _columns.Count - Frame.Width);
			SetNeedsDisplay();
		}

		public override void Redraw(Rect region) {
			base.Redraw(region);
			for (int c = 0; c < Frame.Width; ++c) {
				var v = c >= _columns.Count ? Enumerable.Repeat((Rune)' ', Frame.Height).ToArray() : _columns[c];
				for (int r = 0; r < Frame.Height; ++r) {
					Move(Frame.Width - 1 - c, r);
					Driver.AddRune(v[Frame.Height - 1 - r]);
				}
			}
		}
	}
}
