using System;
using System.Collections.Generic;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// Carries the index of the tab the user picked.
	/// </summary>
	public class ShellTabSelectedEventArgs : EventArgs
	{
		public ShellTabSelectedEventArgs(int index)
		{
			Index = index;
		}

		public int Index { get; }
	}

	/// <summary>
	/// A horizontal strip of text tabs with a Material-style indicator under the selected one.
	/// Used twice by <see cref="ShellWidget"/>: once for the <c>ShellSection</c>s of the current
	/// <c>ShellItem</c>, once for the <c>ShellContent</c>s of the current <c>ShellSection</c>.
	/// </summary>
	/// <remarks>
	/// Choices that are deliberate, each one a trap this port already hit:
	///
	/// * Tabs are <see cref="EventBox"/> + <see cref="Gtk.Label"/>, never <see cref="Gtk.Button"/>.
	///   A themed GTK3 button paints a <c>background-image</c> gradient over whatever colour is set
	///   on it, so a Shell-coloured tab bar would render theme-grey.
	/// * The selection indicator is a <b>real 3px widget</b>, not a CSS <c>border-bottom</c>.
	///   <c>GtkEventBox</c>'s draw handler renders a background but not a frame, so a CSS border on
	///   it is simply never painted.
	/// * Every tab host sets <c>VisibleWindow = true</c> so it owns a GdkWindow and can actually
	///   paint its own background instead of showing the bar's.
	/// * Indicators set <c>NoShowAll</c>, so a parent's <c>ShowAll()</c> cannot light up every tab
	///   at once.
	/// * The selected tab's <b>bold weight is a Pango attribute, not CSS</b>. A CSS
	///   <c>font-weight</c> change does not invalidate a <see cref="Gtk.Label"/>'s cached layout:
	///   the style context reports the new font and the colour repaints, but the label keeps the
	///   size - and, until something else invalidates it, the glyphs - it had while bold. Measured
	///   (<c>scratchpad/shell23-boldprobe2.sh</c>): a label taken bold and back through CSS keeps
	///   its bold natural width (201 → 236 → 236); the same round trip through
	///   <see cref="Pango.AttrList"/> returns to the normal width (215 → 253 → 215). A de-selected
	///   tab was once caught still *drawn* bold in a screenshot, which is the same fault seen at
	///   paint time - it needs no allocation change to recover from, so it is intermittent.
	/// </remarks>
	public class ShellTabBar : EventBox
	{
		public const int TabBarHeight = 40;
		const int IndicatorHeight = 3;

		sealed class Tab
		{
			public EventBox Host;
			public Gtk.Label Label;
			public EventBox Indicator;
		}

		readonly Gtk.Box _box;
		readonly List<Tab> _tabs = new List<Tab>();

		int _selectedIndex = -1;
		Gdk.Color? _background;
		Gdk.Color? _foreground;
		Gdk.Color? _unselectedForeground;

		public ShellTabBar()
		{
			VisibleWindow = true;
			NoShowAll = true;
			HeightRequest = TabBarHeight;

			_box = new Gtk.Box(Gtk.Orientation.Horizontal, 0);

			Add(_box);
		}

		/// <summary>Raised when the user clicks a tab that is not already selected.</summary>
		public event EventHandler<ShellTabSelectedEventArgs> TabSelected;

		public int Count => _tabs.Count;

		/// <summary>The tab hosts, in order - the handle a test needs to click or measure one.</summary>
		public IReadOnlyList<EventBox> TabHosts
		{
			get
			{
				var hosts = new List<EventBox>(_tabs.Count);

				foreach (var tab in _tabs)
					hosts.Add(tab.Host);

				return hosts;
			}
		}

		public int SelectedIndex
		{
			get => _selectedIndex;
			set
			{
				if (_selectedIndex == value)
					return;

				_selectedIndex = value;
				RefreshStyles();
			}
		}

		/// <summary>
		/// Replaces the tabs. Returns without touching anything if the titles are unchanged, so a
		/// structure-changed storm does not rebuild (and un-realize) the strip on every event.
		/// </summary>
		public void SetTabs(IList<string> titles)
		{
			if (titles == null)
				titles = new string[0];

			if (SameTitles(titles))
				return;

			foreach (var tab in _tabs)
			{
				tab.Host.ButtonPressEvent -= OnTabPressed;

				_box.Remove(tab.Host);
				tab.Host.Destroy();
			}

			_tabs.Clear();

			foreach (var title in titles)
			{
				var label = new Gtk.Label(title ?? string.Empty)
				{
					Ellipsize = Pango.EllipsizeMode.End
				};

				var indicator = new EventBox
				{
					VisibleWindow = true,
					NoShowAll = true,
					HeightRequest = IndicatorHeight
				};

				var column = new Gtk.Box(Gtk.Orientation.Vertical, 0);
				column.PackStart(label, true, true, 0);
				column.PackStart(indicator, false, false, 0);

				var host = new EventBox { VisibleWindow = true };
				host.Add(column);
				host.ButtonPressEvent += OnTabPressed;

				_box.PackStart(host, true, true, 0);

				_tabs.Add(new Tab { Host = host, Label = label, Indicator = indicator });
			}

			_box.ShowAll();
			RefreshStyles();
		}

		public void UpdateColors(Gdk.Color? background, Gdk.Color? foreground, Gdk.Color? unselectedForeground)
		{
			_background = background;
			_foreground = foreground;
			_unselectedForeground = unselectedForeground;

			// Color.Default means "let the GTK theme draw it", so the reset is ClearStyle().
			if (_background.HasValue)
				this.SetBackgroundColor(_background.Value);
			else
				this.ClearStyle();

			RefreshStyles();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var tab in _tabs)
					tab.Host.ButtonPressEvent -= OnTabPressed;
			}

			base.Dispose(disposing);
		}

		bool SameTitles(IList<string> titles)
		{
			if (titles.Count != _tabs.Count)
				return false;

			for (var i = 0; i < titles.Count; i++)
			{
				if (!string.Equals(_tabs[i].Label.Text, titles[i] ?? string.Empty, StringComparison.Ordinal))
					return false;
			}

			return true;
		}

		void OnTabPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			for (var i = 0; i < _tabs.Count; i++)
			{
				if (!ReferenceEquals(_tabs[i].Host, o) || i == _selectedIndex)
					continue;

				TabSelected?.Invoke(this, new ShellTabSelectedEventArgs(i));
				return;
			}
		}

		void RefreshStyles()
		{
			// The indicator falls back to the label colour, which is the one guaranteed to read
			// against whatever the bar is painted with.
			var indicatorColor = _foreground ?? new Gdk.Color(33, 118, 210);

			for (var i = 0; i < _tabs.Count; i++)
			{
				var tab = _tabs[i];
				var selected = i == _selectedIndex;

				tab.Host.ClearStyle();

				if (_background.HasValue)
					tab.Host.SetBackgroundColor(_background.Value);

				tab.Label.ClearStyle();

				var color = selected ? _foreground : (_unselectedForeground ?? _foreground);

				if (color.HasValue)
					tab.Label.SetForegroundColor(color.Value);

				// Weight through Pango, never CSS - see the remarks on this class. Setting the
				// attribute list is what clears the label's layout and queues its resize, so the
				// change is actually drawn.
				tab.Label.Attributes = WeightAttributes(selected ? Pango.Weight.Bold : Pango.Weight.Normal);

				tab.Indicator.SetBackgroundColor(indicatorColor);

				if (selected)
					tab.Indicator.Visible = true;
				else
					tab.Indicator.Visible = false;
			}
		}

		/// <summary>A fresh attribute list carrying just a font weight.</summary>
		/// <remarks>
		/// A new list per call on purpose: <c>gtk_label_set_attributes</c> takes its own reference
		/// and a shared list handed to several labels is one lifetime shared between them.
		/// </remarks>
		static Pango.AttrList WeightAttributes(Pango.Weight weight)
		{
			var attributes = new Pango.AttrList();

			attributes.Insert(new Pango.AttrWeight(weight));

			return attributes;
		}
	}
}
