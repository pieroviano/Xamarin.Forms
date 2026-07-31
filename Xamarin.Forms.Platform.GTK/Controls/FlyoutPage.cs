using System;
using Gdk;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	public enum FlyoutLayoutBehaviorType
	{
		Default = 0,
		Popover,
		Split
	}

	public class FlyoutPage : Fixed
	{
		// internal so FlyoutPageRenderer can report matching bounds back to Forms.
		internal const int DefaultFlyoutWidth = 300;

		private Gdk.Rectangle _lastAllocation;
		private bool _isPresented;
		private FlyoutPageFlyoutTitleContainer _titleContainer;
		private EventBox _flyoutContainerWrapper;
		private Box _flyoutContainer;
		private Widget _flyout;
		private Widget _detail;
		private FlyoutLayoutBehaviorType _flyoutBehaviorType;
		private static Pixbuf _hamburgerPixBuf;
		private bool _displayTitle;

		public FlyoutPage()
		{
			_flyoutBehaviorType = FlyoutLayoutBehaviorType.Default;

			// Flyout Stuff
			_flyoutContainerWrapper = new EventBox();

			// The wrapper's visibility is driven by RefreshFlyoutVisibility, so it must survive
			// the ShowAll() calls the renderers make on their parents.
			_flyoutContainerWrapper.NoShowAll = true;
			_flyoutContainer = new Box(Gtk.Orientation.Vertical, 0);
			_titleContainer = new FlyoutPageFlyoutTitleContainer();
			_titleContainer.HamburguerClicked += OnHamburgerClicked;
			_titleContainer.HeightRequest = GtkToolbarConstants.ToolbarHeight;
			_flyoutContainer.PackStart(_titleContainer, false, true, 0);

			_flyout = new EventBox();
			_flyoutContainer.PackEnd(_flyout, false, true, 0);
			_flyoutContainerWrapper.Add(_flyoutContainer);

			// Detail Stuff
			_detail = new EventBox();

			Add(_detail);
			Add(_flyoutContainerWrapper);
		}

		public FlyoutLayoutBehaviorType FlyoutLayoutBehaviorType
		{
			get
			{
				return _flyoutBehaviorType;
			}

			set
			{
				if (_flyoutBehaviorType != value)
				{
					_flyoutBehaviorType = value;
					RefreshFlyoutLayoutBehavior(_flyoutBehaviorType);
				}
			}
		}

		public Widget Flyout
		{
			get
			{
				return _flyout;
			}

			set
			{
				RefreshFlyout(value);
			}
		}

		public Widget Detail
		{
			get
			{
				return _detail;
			}

			set
			{
				RefreshDetail(value);
			}
		}

		public bool IsPresented
		{
			get
			{
				return _isPresented;
			}

			set
			{
				RefreshPresented(value);
				NotifyIsPresentedChanged();
			}
		}

		public string FlyoutTitle
		{
			get
			{
				return _titleContainer.Title;
			}

			set
			{
				_titleContainer.Title = value ?? string.Empty;
			}
		}

		public bool DisplayTitle
		{
			get
			{
				return _displayTitle;
			}

			set
			{
				RefreshDisplayTitle(value);
			}
		}

		public static Pixbuf HamburgerPixBuf
		{
			get
			{
				try
				{
					if (_hamburgerPixBuf == null)
					{
						_hamburgerPixBuf = new Pixbuf("./Resources/hamburger.png");
					}

					return _hamburgerPixBuf;
				}
				catch
				{
					return null;
				}
			}
			set
			{
				_hamburgerPixBuf = value;
			}
		}

		public void UpdateBarTextColor(Gdk.Color? barTextColor)
		{
			if (_titleContainer != null)
			{
				_titleContainer.UpdateTitleColor(barTextColor);
			}
		}

		public void UpdateBarBackgroundColor(Gdk.Color? barBackgroundColor)
		{
			if (_titleContainer != null)
			{
				_titleContainer.UpdateBackgroundColor(barBackgroundColor);
			}
		}

		public void UpdateHamburguerIcon(Pixbuf hamburguerIcon)
		{
			HamburgerPixBuf = hamburguerIcon;

			if (_titleContainer != null)
			{
				_titleContainer.HamburgerPixBuf = HamburgerPixBuf;
			}
		}

		public event EventHandler IsPresentedChanged;

		bool _sizeUpdateQueued;

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_lastAllocation != allocation)
			{
				_lastAllocation = allocation;
			}

			// Deferred: WidthRequest/HeightRequest and RefreshFlyoutLayoutBehavior's MoveTo
			// all queue a resize, which GTK3 discards when queued from inside size-allocate.
			// Applied inline, the detail widget kept its natural size, and the detail page's
			// own renderer then pushed that natural size back into Forms - overwriting the
			// correct DetailBounds the FlyoutPageRenderer had just set.
			if (_sizeUpdateQueued)
				return;

			_sizeUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_sizeUpdateQueued = false;

				if (_flyout != null && _detail != null)
				{
					_flyout.WidthRequest = DefaultFlyoutWidth;
					_flyout.HeightRequest = _detail.HeightRequest = _lastAllocation.Height;

					// The WRAPPER must be constrained too, not just the flyout inside it. It is an
					// EventBox around a Box holding the title bar and the flyout page, so its
					// natural width can exceed DefaultFlyoutWidth - and since hiding it means moving
					// it to -DefaultFlyoutWidth, any excess stays visible as a strip painted over
					// the detail (a ~45px red band down the left of the ControlGallery).
					if (_flyoutContainerWrapper != null)
					{
						_flyoutContainerWrapper.WidthRequest = DefaultFlyoutWidth;
						_flyoutContainerWrapper.HeightRequest = _lastAllocation.Height;
					}

					RefreshFlyoutLayoutBehavior(_flyoutBehaviorType);
				}

				return false;
			});
		}



		private void RefreshFlyoutLayoutBehavior(FlyoutLayoutBehaviorType flyoutBehaviorType)
		{
			int detailWidthRequest = 0;
			Gdk.Point point = default(Gdk.Point);

			switch (_flyoutBehaviorType)
			{
				case FlyoutLayoutBehaviorType.Split:
					detailWidthRequest = _lastAllocation.Width - DefaultFlyoutWidth;
					point = new Gdk.Point(_flyout.WidthRequest, 0);
					break;
				case FlyoutLayoutBehaviorType.Default:
				case FlyoutLayoutBehaviorType.Popover:
					detailWidthRequest = _lastAllocation.Width;
					point = new Gdk.Point(0, 0);
					break;
			}

			// Position the flyout as well as the detail. RefreshPresented only runs when
			// IsPresented CHANGES, so in Popover/Default mode nothing ever moved the flyout
			// off-screen at start-up: it stayed at x=0 painting over the left edge of the
			// detail (in the ControlGallery, a red band clipping the first ~70px of every row).
			if (_flyoutContainerWrapper != null)
			{
				var flyoutX = FlyoutVisible ? 0 : -DefaultFlyoutWidth;

				_flyoutContainerWrapper.MoveTo(flyoutX, 0);
				RefreshFlyoutVisibility();
			}

			if (detailWidthRequest >= 0)
			{
				_detail.WidthRequest = detailWidthRequest;
				_detail.MoveTo(point.X, point.Y);
			}
		}

		// True when the flyout should be on screen: always in Split, only while presented otherwise.
		private bool FlyoutVisible =>
			_flyoutBehaviorType == FlyoutLayoutBehaviorType.Split || _isPresented;

		// Parking the wrapper at x=-DefaultFlyoutWidth is NOT enough to hide it. GTK3 caches a
		// child's allocation, and moving a Gtk.Fixed child does not reliably re-allocate it: the
		// ControlGallery left the flyout's child property at -300 while its actual allocation
		// stayed at ~-205, leaking an ~80px strip of the red flyout over the detail and clipping
		// the first characters of every row. An explicit QueueResize() on the Fixed did not
		// dislodge it either. Hiding the widget is both reliable and correct - an off-screen
		// child still paints and still takes events.
		private void RefreshFlyoutVisibility()
		{
			if (_flyoutContainerWrapper == null)
				return;

			if (FlyoutVisible)
			{
				// ShowAll() on the wrapper is a no-op: gtk_widget_show_all skips any widget with
				// no_show_all set, and the wrapper sets it so a parent's ShowAll cannot reveal a
				// dismissed flyout. gtk_widget_show does not consult the flag, so show the wrapper
				// itself explicitly and ShowAll the contents through the inner container.
				_flyoutContainer.ShowAll();
				_flyoutContainerWrapper.Show();
				_flyoutContainerWrapper.Window?.Raise();
			}
			else
			{
				_flyoutContainerWrapper.Hide();
			}
		}

		private void RefreshFlyout(Widget newFlyout)
		{
			if (_flyout != null)
			{
				_flyoutContainer.RemoveFromContainer(_flyout);
			}

			UpdateHamburguerIcon(HamburgerPixBuf);
			_flyout = newFlyout;
			_flyoutContainer.PackEnd(newFlyout, false, true, 0);
			_flyout.ShowAll();
		}

		private void RefreshDetail(Widget newDetail)
		{
			if (_detail != null)
			{
				this.RemoveFromContainer(_detail);
			}

			_detail = newDetail;

			Add(_detail);

			// Re-add so the flyout stays above the detail in the Fixed's child order. Gtk.Fixed.Add
			// drops the child at (0,0), so the position has to be re-applied afterwards or the
			// flyout reappears over the detail's left edge.
			Remove(_flyoutContainerWrapper);
			Add(_flyoutContainerWrapper);

			_detail.ShowAll();

			_flyoutContainerWrapper.MoveTo(FlyoutVisible ? 0 : -DefaultFlyoutWidth, 0);
			RefreshFlyoutVisibility(); // also raises the GdkWindow, forcing Flyout to be on top
		}

		// The slide-in/slide-out animation is deliberately gone under GTK3. It drove the flyout by
		// repeatedly moving a Gtk.Fixed child, and a moved Fixed child does not reliably pick up a
		// new allocation: the widget's child x reached its target while the actual allocation stayed
		// at whatever intermediate frame was last allocated (measured: child x=-300, allocation
		// x=-205, so an ~95px strip of the flyout stayed painted over the detail). Showing a widget,
		// by contrast, always allocates it afresh - so position first, then toggle visibility.
		// Restoring the animation needs a mechanism that re-allocates each frame (M6).
		private void RefreshPresented(bool isPresented)
		{
			_isPresented = isPresented;

			if (_flyoutBehaviorType == FlyoutLayoutBehaviorType.Split)
				return;

			if (FlyoutVisible)
			{
				// Position while still hidden, then show, so the show applies the new position.
				_flyoutContainerWrapper.MoveTo(0, 0);
				RefreshFlyoutVisibility();
			}
			else
			{
				RefreshFlyoutVisibility();
				_flyoutContainerWrapper.MoveTo(-DefaultFlyoutWidth, 0);
			}
		}

		private void RefreshDisplayTitle(bool value = true)
		{
			_displayTitle = value;

			_flyoutContainer.RemoveFromContainer(_titleContainer);

			if (_displayTitle)
			{
				_flyoutContainer.PackStart(_titleContainer, false, true, 0);
			}
		}

		private void OnHamburgerClicked(object sender, EventArgs e)
		{
			IsPresented = !IsPresented;
		}

		private void NotifyIsPresentedChanged()
		{
			IsPresentedChanged?.Invoke(this, EventArgs.Empty);
		}

		private class FlyoutPageFlyoutTitleContainer : EventBox
		{
			private Box _root;
			private ToolButton _hamburguerButton;
			private Gtk.Label _titleLabel;
			private Gtk.Image _hamburguerIcon;
			private Gdk.Color _defaultTextColor;
			private Gdk.Color _defaultBackgroundColor;

			public FlyoutPageFlyoutTitleContainer()
			{
				_defaultBackgroundColor = this.GetDefaultBackgroundColor(Gtk.StateFlags.Normal);

				_root = new Box(Gtk.Orientation.Horizontal, 0);
				_hamburguerIcon = new Gtk.Image();

				try
				{
					_hamburguerIcon = new Gtk.Image(HamburgerPixBuf);
				}
				catch (Exception ex)
				{
					Internals.Log.Warning("FlyoutPage HamburguerIcon", "Could not load hamburguer icon: {0}", ex);
				}

				_hamburguerButton = new ToolButton(_hamburguerIcon, string.Empty);
				_hamburguerButton.HeightRequest = GtkToolbarConstants.ToolbarItemHeight;
				_hamburguerButton.WidthRequest = GtkToolbarConstants.ToolbarItemWidth;
				_hamburguerButton.Clicked += OnHamburguerButtonClicked;

				_titleLabel = new Gtk.Label();
				_defaultTextColor = _titleLabel.GetDefaultForegroundColor(Gtk.StateFlags.Normal);

				_root.PackStart(_hamburguerButton, false, false, GtkToolbarConstants.ToolbarItemSpacing);
				_root.PackStart(_titleLabel, false, false, 25);

				Add(_root);
			}

			public string Title
			{
				get
				{
					return _titleLabel.Text;
				}

				set
				{
					_titleLabel.Text = value ?? string.Empty;
				}
			}

			public Pixbuf HamburgerPixBuf
			{
				get
				{
					return _hamburguerIcon.Pixbuf;
				}

				set
				{
					_hamburguerIcon.Pixbuf = value ?? null;
				}
			}

			public void UpdateTitleColor(Gdk.Color? titleColor)
			{
				if (_titleLabel != null)
				{
					if (titleColor.HasValue)
					{
						_titleLabel.SetForegroundColor(titleColor.Value, StateType.Normal);
					}
					else
					{
						_titleLabel.SetForegroundColor(_defaultTextColor, StateType.Normal);
					}
				}
			}

			public void UpdateBackgroundColor(Gdk.Color? backgroundColor)
			{
				if (_root == null)
				{
					return;
				}

				if (backgroundColor.HasValue)
				{
					StyleExtensions.SetBackgroundColor(this, backgroundColor.Value);
					_root.SetBackgroundColor(backgroundColor.Value, StateType.Normal);
				}
				else
				{
					StyleExtensions.SetBackgroundColor(this, _defaultBackgroundColor);
					_root.SetBackgroundColor(_defaultBackgroundColor, StateType.Normal);
				}
			}

			public event EventHandler HamburguerClicked;

			private void OnHamburguerButtonClicked(object sender, EventArgs e)
			{
				HamburguerClicked?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	public class MasterDetailPage : FlyoutPage
	{

		public string MasterTitle
		{
			get => base.FlyoutTitle;

			set => base.FlyoutTitle = value;
		}
	}
}
