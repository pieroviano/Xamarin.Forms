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

		// Slide duration for presenting/dismissing the flyout in Popover/Default mode.
		const uint FlyoutAnimationMilliseconds = 250;

		private Gdk.Rectangle _lastAllocation;
		private bool _isPresented;
		private FlyoutPageFlyoutTitleContainer _titleContainer;
		private EventBox _flyoutContainerWrapper;
		private Revealer _flyoutRevealer;
		private uint _slideTick;
		private uint _slideElapsed;
		private bool _slideTarget;
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

			// The slide is driven by a Gtk.Revealer, NOT by moving the wrapper inside the Fixed.
			// A Revealer animates its own preferred width and queues a resize every frame, so the
			// Fixed genuinely re-allocates the wrapper each frame (measured: 1 -> 69 -> 126 -> 173
			// -> 211 -> 240 -> ... -> 300 px). Moving a Gtk.Fixed child, which is how the GTK 2
			// animation worked, does not re-allocate it at all - see RefreshFlyoutVisibility.
			_flyoutRevealer = new Revealer
			{
				TransitionType = RevealerTransitionType.SlideRight,
				TransitionDuration = FlyoutAnimationMilliseconds,
				RevealChild = false
			};

			_flyoutContainer = new Box(Gtk.Orientation.Vertical, 0);
			_titleContainer = new FlyoutPageFlyoutTitleContainer();
			_titleContainer.HamburguerClicked += OnHamburgerClicked;
			_titleContainer.HeightRequest = GtkToolbarConstants.ToolbarHeight;
			_flyoutContainer.PackStart(_titleContainer, false, true, 0);

			_flyout = new EventBox();
			_flyoutContainer.PackEnd(_flyout, false, true, 0);
			_flyoutRevealer.Add(_flyoutContainer);
			_flyoutContainerWrapper.Add(_flyoutRevealer);

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

		/// <summary>
		/// Sizes the flyout page to the space left inside its container.
		/// </summary>
		/// <remarks>
		/// The flyout shares that container with the hamburger title bar, which is present in
		/// Popover/Default mode and absent in Split. Asking for the full height regardless
		/// overflowed the Box by exactly the toolbar height and pushed the flyout page's content
		/// off the bottom - visible in Popover mode as a band of bare page background under it.
		/// </remarks>
		private void ApplyFlyoutSizeRequests()
		{
			if (_flyout == null || _lastAllocation.Height <= 1)
				return;

			var titleHeight = _displayTitle ? GtkToolbarConstants.ToolbarHeight : 0;

			_flyout.WidthRequest = DefaultFlyoutWidth;
			_flyout.HeightRequest = Math.Max(1, _lastAllocation.Height - titleHeight);
		}

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
					_detail.HeightRequest = _lastAllocation.Height;
					ApplyFlyoutSizeRequests();

					// The CONTAINER must be constrained too, not just the flyout page inside it: it
					// is a Box holding the title bar and the flyout page, so its natural width can
					// exceed DefaultFlyoutWidth, and the excess used to stay visible as a strip
					// painted over the detail (a ~45px red band down the left of the ControlGallery).
					//
					// This request goes on the container INSIDE the revealer, never on the wrapper.
					// The wrapper's preferred width has to stay free to follow the revealer's
					// animated width; a WidthRequest on the wrapper would pin it at 300 and the
					// slide would be invisible.
					if (_flyoutContainer != null)
					{
						_flyoutContainer.WidthRequest = DefaultFlyoutWidth;
						_flyoutContainer.HeightRequest = _lastAllocation.Height;
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
				MoveWrapperToRestingPosition();
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

		/// <summary>
		/// Parks the wrapper where the CURRENT state says it belongs - unless a slide heading for
		/// that same state is still running, in which case the slide owns the position until it ends.
		/// </summary>
		/// <remarks>
		/// Both callers run from the main loop and can therefore land in the middle of a 250ms
		/// transition, and an unguarded MoveTo there teleports the flyout instead of sliding it:
		/// <list type="bullet">
		/// <item>a window resize, via <c>RefreshFlyoutLayoutBehavior</c> on the allocation idle -
		/// measured at fixedX=-300 with 0px of flyout painted, 100ms into a slide with 150ms left;</item>
		/// <item>a detail-page swap, via <c>RefreshDetail</c>. <c>FlyoutPageRenderer.UpdateFlyoutPage</c>
		/// assigns <c>Widget.Detail</c> inside a <c>Gtk.Application.Invoke</c> while
		/// <c>UpdateIsPresented</c> runs synchronously, so the ORDINARY "tap a flyout item"
		/// gesture - <c>Detail = page; IsPresented = false;</c> - delivers the detail swap after the
		/// dismiss slide has already started. This is the common path, not an edge case.</item>
		/// </list>
		/// Skipping the move is safe for both slide directions: a dismiss slides in place from x=0
		/// and <c>HideFlyoutNow</c> parks it when the slide ends, and a present wants x=0 anyway -
		/// which is also where <c>Gtk.Fixed.Add</c> drops a re-added child.
		/// </remarks>
		private void MoveWrapperToRestingPosition()
		{
			if (_flyoutContainerWrapper == null)
				return;

			if (_slideTick != 0 && _slideTarget == FlyoutVisible)
				return;

			_flyoutContainerWrapper.MoveTo(FlyoutVisible ? 0 : -DefaultFlyoutWidth, 0);
		}

		// Parking the wrapper at x=-DefaultFlyoutWidth is NOT enough to hide it. GTK3 caches a
		// child's allocation, and moving a Gtk.Fixed child does not reliably re-allocate it: the
		// ControlGallery left the flyout's child property at -300 while its actual allocation
		// stayed at ~-205, leaking an ~80px strip of the red flyout over the detail and clipping
		// the first characters of every row. An explicit QueueResize() on the Fixed did not
		// dislodge it either. Hiding the widget is both reliable and correct - an off-screen
		// child still paints and still takes events.
		//
		// The slide is layered on top of that, and deliberately does not replace it: the revealer
		// animates the width, and the wrapper is still hidden (and parked) once the transition has
		// finished. A collapsed revealer is 1px wide, not 0 - and 1px of an EventBox with its own
		// GdkWindow still paints over the detail and still swallows clicks.
		private void RefreshFlyoutVisibility(bool animate = false)
		{
			if (_flyoutContainerWrapper == null || _flyoutRevealer == null)
				return;

			var show = FlyoutVisible;

			// A slide already heading for this state must be left alone. The non-animated callers
			// (RefreshFlyoutLayoutBehavior, RefreshDetail) run from an idle on every allocation, so
			// without this they would snap the transition duration to 0 mid-slide.
			if (_slideTick != 0 && _slideTarget == show)
				return;

			StopSlide();

			var slide = animate && (show || _flyoutContainerWrapper.Visible);

			_flyoutRevealer.TransitionDuration = slide ? FlyoutAnimationMilliseconds : 0;

			if (show)
			{
				// ShowAll() on the wrapper is a no-op: gtk_widget_show_all skips any widget with
				// no_show_all set, and the wrapper sets it so a parent's ShowAll cannot reveal a
				// dismissed flyout. gtk_widget_show does not consult the flag, so show the wrapper
				// itself explicitly and ShowAll the contents through the revealer.
				_flyoutRevealer.ShowAll();
				_flyoutContainerWrapper.Show();
				_flyoutContainerWrapper.Window?.Raise();
			}

			if (slide)
			{
				StartSlide(show);
				return;
			}

			_flyoutRevealer.RevealChild = show;

			if (show)
				AllocateWrapperToRevealer();
			else
				HideFlyoutNow();
		}

		/// <summary>
		/// Allocates the flyout wrapper to the revealer's current (animated) preferred width.
		/// </summary>
		/// <remarks>
		/// This is exactly what this Gtk.Fixed's own size_allocate would do for the child - it is
		/// driven from here because the page subtree's resize propagation is wedged: a
		/// QueueResize raised on the wrapper, on this Fixed, or on the container above it changes
		/// nothing, and only a QueueResize on the *toplevel* re-allocates anything. Without this
		/// the revealer's preferred width climbed 56 -> 299 -> 300 while the widget's allocation
		/// stayed at 1px, i.e. the flyout animated invisibly. See the plan, section 6, for the
		/// wedge itself, which is a separate defect.
		/// </remarks>
		private void AllocateWrapperToRevealer()
		{
			if (_flyoutContainerWrapper == null || _flyoutRevealer == null)
				return;

			if (_flyoutContainerWrapper.Handle == IntPtr.Zero || !_flyoutContainerWrapper.Visible)
				return;

			_flyoutRevealer.GetPreferredWidth(out int width, out _);
			_flyoutRevealer.GetPreferredHeight(out int height, out _);

			if (_lastAllocation.Height > 1)
				height = _lastAllocation.Height;

			var x = (int)ChildGetProperty(_flyoutContainerWrapper, "x").Val;
			var y = (int)ChildGetProperty(_flyoutContainerWrapper, "y").Val;

			_flyoutContainerWrapper.SizeAllocate(new Gdk.Rectangle(
				Allocation.X + x, Allocation.Y + y, Math.Max(1, width), Math.Max(1, height)));
		}

		// ~60fps for the length of the transition, plus a little slack so the last frame lands on
		// the exact end state rather than one step short of it.
		private void StartSlide(bool reveal)
		{
			_slideElapsed = 0;
			_slideTarget = reveal;

			_slideTick = GLib.Timeout.Add(16, () =>
			{
				if (_slideElapsed == 0)
				{
					// The reveal is requested from the FIRST TICK, not from the call site.
					// gtk_revealer_start_animation snaps straight to the target when the widget is
					// not yet mapped, and on the very first present the wrapper is shown in the
					// same main-loop iteration - so that slide, and only that one, played
					// instantly (measured: 0 intermediate widths on present #1, 7 on present #2).
					// One tick later the wrapper is mapped and the transition runs properly.
					_flyoutRevealer.RevealChild = reveal;
				}

				_slideElapsed += 16;

				AllocateWrapperToRevealer();

				if (_slideElapsed < FlyoutAnimationMilliseconds + 80)
					return true;

				_slideTick = 0;

				if (FlyoutVisible)
					AllocateWrapperToRevealer();
				else
					HideFlyoutNow();

				return false;
			});
		}

		private void StopSlide()
		{
			if (_slideTick == 0)
				return;

			GLib.Source.Remove(_slideTick);
			_slideTick = 0;
		}

		// Hide first, then park. Parking a *visible* child would move it instantly and skip the
		// slide; parking a hidden one costs nothing and keeps a stray Show() harmless.
		private void HideFlyoutNow()
		{
			if (_flyoutContainerWrapper == null || _flyoutContainerWrapper.Handle == IntPtr.Zero)
				return;

			_flyoutContainerWrapper.Hide();
			_flyoutContainerWrapper.MoveTo(-DefaultFlyoutWidth, 0);
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

			MoveWrapperToRestingPosition();
			RefreshFlyoutVisibility(); // also raises the GdkWindow, forcing Flyout to be on top
		}

		// The GTK 2 animation drove the flyout by repeatedly moving a Gtk.Fixed child, and a moved
		// Fixed child does not reliably pick up a new allocation: the child x reached its target
		// while the actual allocation stayed at whatever intermediate frame was last allocated
		// (measured: child x=-300, allocation x=-205, so an ~95px strip of the flyout stayed
		// painted over the detail). This is the only place the flyout is animated, and it animates
		// the revealer - which re-allocates for real - never the position.
		private void RefreshPresented(bool isPresented)
		{
			_isPresented = isPresented;

			if (_flyoutBehaviorType == FlyoutLayoutBehaviorType.Split)
				return;

			// Position while still hidden, then show, so the show applies the new position. The
			// dismiss path parks the wrapper itself, once the slide has finished.
			if (FlyoutVisible)
				_flyoutContainerWrapper.MoveTo(0, 0);

			RefreshFlyoutVisibility(animate: true);
		}

		private void RefreshDisplayTitle(bool value = true)
		{
			_displayTitle = value;

			_flyoutContainer.RemoveFromContainer(_titleContainer);

			if (_displayTitle)
			{
				_flyoutContainer.PackStart(_titleContainer, false, true, 0);
			}

			// Adding or removing the title bar changes how much room is left for the flyout page.
			ApplyFlyoutSizeRequests();
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
