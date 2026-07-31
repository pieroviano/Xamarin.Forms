using System;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Controls
{
	/// <summary>
	/// How the flyout relates to the content, mirroring <see cref="Xamarin.Forms.FlyoutBehavior"/>.
	/// </summary>
	public enum ShellFlyoutBehaviorType
	{
		Disabled = 0,
		Flyout,
		Locked
	}

	/// <summary>
	/// The native side of <c>ShellRenderer</c>: a nav bar (flyout toggle or back arrow, plus title),
	/// two <see cref="ShellTabBar"/> strips, and a <see cref="Gtk.Overlay"/> holding the content
	/// <see cref="Gtk.Stack"/> with the flyout panel over it.
	/// </summary>
	/// <remarks>
	/// Deliberate structural choices, each of which cost time elsewhere in this port:
	///
	/// * The flyout is an <b>overlay child</b> of a <see cref="Gtk.Overlay"/>, not a child of a
	///   <see cref="Gtk.Fixed"/>. <c>Controls.FlyoutPage</c> uses a Fixed and had to drop its slide
	///   animation because a moved Fixed child does not reliably re-allocate. Overlay allocates its
	///   overlay children itself from their align/size-request, so there is no geometry to move and
	///   the whole class of bug does not arise.
	/// * The flyout is hidden by <b>hiding it</b>, never by parking it off-screen (the rule
	///   <c>Controls.FlyoutPage</c> had to learn the hard way), and its wrapper is an
	///   <see cref="Gtk.EventBox"/> with <c>VisibleWindow = true</c> so it owns a real GdkWindow and
	///   paints above the content rather than under it.
	/// * <c>NoShowAll</c> on the wrapper stops a parent's <c>ShowAll()</c> from re-revealing a
	///   dismissed flyout; the contents are shown through the inner box, which does not set the flag.
	/// * The toggle is an EventBox + Label rather than a <see cref="Gtk.Button"/>. A themed GTK3
	///   button paints a <c>background-image</c> gradient over any colour we set, which is the
	///   SwipeView/Button trap; a plain EventBox simply shows the colours Shell asked for.
	/// </remarks>
	public class ShellWidget : Gtk.Box
	{
		public const int DefaultFlyoutWidth = 300;
		public const int NavBarHeight = 48;

		readonly EventBox _navBar;
		readonly Gtk.Box _navBarBox;
		readonly EventBox _toggleBox;
		readonly Gtk.Label _toggleLabel;
		readonly EventBox _backBox;
		readonly Gtk.Label _backLabel;
		readonly Gtk.Label _titleLabel;

		readonly ShellTabBar _sectionTabs;
		readonly ShellTabBar _contentTabs;

		readonly Gtk.Overlay _body;
		readonly Gtk.Stack _contentStack;

		readonly EventBox _flyoutWrapper;
		readonly Gtk.Box _flyoutBox;
		readonly EventBox _flyoutHeaderHost;
		readonly Gtk.ScrolledWindow _flyoutScroller;
		readonly Gtk.Viewport _flyoutViewport;
		readonly Gtk.Box _flyoutItemsBox;
		readonly EventBox _flyoutFooterHost;

		bool _isPresented;
		bool _navBarVisible = true;
		bool _backVisible;
		bool _sectionTabsVisible;
		bool _contentTabsVisible;
		int _flyoutWidth = DefaultFlyoutWidth;
		ShellFlyoutBehaviorType _behavior = ShellFlyoutBehaviorType.Flyout;

		public ShellWidget()
			: base(Gtk.Orientation.Vertical, 0)
		{
			// ---- nav bar -----------------------------------------------------------------
			_navBar = new EventBox { VisibleWindow = true };
			_navBar.HeightRequest = NavBarHeight;
			_navBar.NoShowAll = true;

			_navBarBox = new Gtk.Box(Gtk.Orientation.Horizontal, 0);

			_toggleLabel = new Gtk.Label("☰");
			_toggleBox = new EventBox { VisibleWindow = false };
			_toggleBox.NoShowAll = true;
			_toggleBox.WidthRequest = 32;
			_toggleBox.Add(_toggleLabel);
			_toggleBox.ButtonPressEvent += OnTogglePressed;

			// The back arrow takes the hamburger's slot: a pushed page is reached by going back,
			// not by picking a flyout item, which is what every other Shell backend shows too.
			_backLabel = new Gtk.Label("←");
			_backBox = new EventBox { VisibleWindow = false };
			_backBox.NoShowAll = true;
			_backBox.WidthRequest = 32;
			_backBox.Add(_backLabel);
			_backBox.ButtonPressEvent += OnBackPressed;

			_titleLabel = new Gtk.Label(string.Empty) { Xalign = 0f };

			_navBarBox.PackStart(_backBox, false, false, 10);
			_navBarBox.PackStart(_toggleBox, false, false, 10);
			_navBarBox.PackStart(_titleLabel, false, false, 6);
			_navBar.Add(_navBarBox);

			PackStart(_navBar, false, false, 0);

			// ---- tab bars ----------------------------------------------------------------
			// Two levels, exactly as Shell models them: the ShellSections of the current ShellItem
			// (what Android draws as bottom tabs) and the ShellContents of the current ShellSection
			// (what it draws as top tabs). Both live above the body so neither overlaps content.
			_sectionTabs = new ShellTabBar();
			_contentTabs = new ShellTabBar();

			PackStart(_sectionTabs, false, false, 0);
			PackStart(_contentTabs, false, false, 0);

			// ---- body: content + flyout overlay ------------------------------------------
			_body = new Gtk.Overlay();

			_contentStack = new Gtk.Stack
			{
				// Homogeneous would size the stack to the largest page it has ever shown, so a
				// single tall page would then stretch every other one.
				Homogeneous = false,
				Hhomogeneous = false,
				Vhomogeneous = false,
				TransitionType = StackTransitionType.None
			};

			_body.Add(_contentStack);

			_flyoutWrapper = new EventBox
			{
				VisibleWindow = true,
				NoShowAll = true,
				Halign = Align.Start,
				Valign = Align.Fill,
				WidthRequest = _flyoutWidth
			};

			_flyoutBox = new Gtk.Box(Gtk.Orientation.Vertical, 0);

			_flyoutHeaderHost = new EventBox { VisibleWindow = false };
			_flyoutFooterHost = new EventBox { VisibleWindow = false };

			_flyoutItemsBox = new Gtk.Box(Gtk.Orientation.Vertical, 0);

			// The viewport is created here rather than left to ScrolledWindow.Add so the flyout
			// background colour can be applied to it: a CssProvider added to a widget's own
			// StyleContext styles that widget only, so colouring the ScrolledWindow would leave the
			// viewport inside it painting the theme's background over the top.
			_flyoutViewport = new Gtk.Viewport { ShadowType = ShadowType.None };
			_flyoutViewport.Add(_flyoutItemsBox);

			_flyoutScroller = new Gtk.ScrolledWindow();
			_flyoutScroller.SetPolicy(PolicyType.Never, PolicyType.Automatic);
			_flyoutScroller.Add(_flyoutViewport);

			_flyoutBox.PackStart(_flyoutHeaderHost, false, false, 0);
			_flyoutBox.PackStart(_flyoutScroller, true, true, 0);
			_flyoutBox.PackStart(_flyoutFooterHost, false, false, 0);

			_flyoutWrapper.Add(_flyoutBox);
			_body.AddOverlay(_flyoutWrapper);

			PackStart(_body, true, true, 0);

			RefreshNavBarVisibility();
			RefreshFlyoutVisibility();
			RefreshTabBars();
		}

		/// <summary>Raised when the user toggles the flyout from the nav bar.</summary>
		public event EventHandler IsPresentedChanged;

		/// <summary>Raised when the user clicks the nav bar's back arrow.</summary>
		public event EventHandler BackRequested;

		/// <summary>Raised on idle after a size-allocate, so the Forms-side layout pass can run
		/// somewhere GTK3 will not discard the resizes it queues.</summary>
		public event EventHandler LayoutRefreshed;

		public Gtk.Stack ContentStack => _contentStack;

		public Gtk.Box FlyoutItemsBox => _flyoutItemsBox;

		public EventBox FlyoutHeaderHost => _flyoutHeaderHost;

		public EventBox FlyoutFooterHost => _flyoutFooterHost;

		public Gtk.ScrolledWindow FlyoutScroller => _flyoutScroller;

		/// <summary>The tab strip over the current <c>ShellItem</c>'s <c>ShellSection</c>s.</summary>
		public ShellTabBar SectionTabs => _sectionTabs;

		/// <summary>The tab strip over the current <c>ShellSection</c>'s <c>ShellContent</c>s.</summary>
		public ShellTabBar ContentTabs => _contentTabs;

		public bool SectionTabsVisible
		{
			get => _sectionTabsVisible;
			set
			{
				if (_sectionTabsVisible == value)
					return;

				_sectionTabsVisible = value;
				RefreshTabBars();
			}
		}

		public bool ContentTabsVisible
		{
			get => _contentTabsVisible;
			set
			{
				if (_contentTabsVisible == value)
					return;

				_contentTabsVisible = value;
				RefreshTabBars();
			}
		}

		/// <summary>Whether the nav bar shows a back arrow in place of the flyout toggle.</summary>
		public bool BackVisible
		{
			get => _backVisible;
			set
			{
				if (_backVisible == value)
					return;

				_backVisible = value;
				RefreshNavBarVisibility();
			}
		}

		public string BackText
		{
			get => _backLabel.Text;
			set => _backLabel.Text = string.IsNullOrEmpty(value) ? "←" : value;
		}

		public string Title
		{
			get => _titleLabel.Text;
			set => _titleLabel.Text = value ?? string.Empty;
		}

		public bool NavBarVisible
		{
			get => _navBarVisible;
			set
			{
				if (_navBarVisible == value)
					return;

				_navBarVisible = value;
				RefreshNavBarVisibility();
			}
		}

		public int FlyoutWidth
		{
			get => _flyoutWidth;
			set
			{
				var width = value <= 0 ? DefaultFlyoutWidth : value;

				if (_flyoutWidth == width)
					return;

				_flyoutWidth = width;
				_flyoutWrapper.WidthRequest = width;
				RefreshBehavior();
			}
		}

		public ShellFlyoutBehaviorType FlyoutBehaviorType
		{
			get => _behavior;
			set
			{
				if (_behavior == value)
					return;

				_behavior = value;
				RefreshBehavior();
			}
		}

		/// <summary>
		/// Whether the flyout is on screen. In <see cref="ShellFlyoutBehaviorType.Locked"/> it always
		/// is; in <see cref="ShellFlyoutBehaviorType.Disabled"/> it never is.
		/// </summary>
		public bool IsPresented
		{
			get => _isPresented;
			set
			{
				if (_isPresented == value)
					return;

				_isPresented = value;
				RefreshFlyoutVisibility();
			}
		}

		public bool FlyoutVisible =>
			_behavior == ShellFlyoutBehaviorType.Locked ||
			(_behavior == ShellFlyoutBehaviorType.Flyout && _isPresented);

		public void UpdateNavBarColors(Gdk.Color? background, Gdk.Color? foreground)
		{
			// Color.Default means "let the GTK theme draw it", so the reset is ClearStyle().
			if (background.HasValue)
				_navBar.SetBackgroundColor(background.Value);
			else
				_navBar.ClearStyle();

			if (foreground.HasValue)
			{
				_titleLabel.SetForegroundColor(foreground.Value);
				_toggleLabel.SetForegroundColor(foreground.Value);
				_backLabel.SetForegroundColor(foreground.Value);
			}
			else
			{
				_titleLabel.ClearStyle();
				_toggleLabel.ClearStyle();
				_backLabel.ClearStyle();
			}
		}

		public void UpdateTabBarColors(Gdk.Color? background, Gdk.Color? foreground, Gdk.Color? unselected)
		{
			_sectionTabs.UpdateColors(background, foreground, unselected);
			_contentTabs.UpdateColors(background, foreground, unselected);
		}

		public void UpdateFlyoutBackgroundColor(Gdk.Color? background)
		{
			if (background.HasValue)
			{
				_flyoutWrapper.SetBackgroundColor(background.Value);
				_flyoutScroller.SetBackgroundColor(background.Value);
				_flyoutViewport.SetBackgroundColor(background.Value);
			}
			else
			{
				_flyoutWrapper.ClearStyle();
				_flyoutScroller.ClearStyle();
				_flyoutViewport.ClearStyle();
			}
		}

		/// <summary>
		/// Reports the size the renderer asked for as both the minimum and the natural size.
		/// </summary>
		/// <remarks>
		/// This widget lives inside <c>Controls.Page</c>'s <see cref="Gtk.Fixed"/>, and a Fixed
		/// allocates each child its <b>natural</b> size. In Locked mode the content stack carries a
		/// 300px start margin, which pushed the natural width to window+300 - and the Fixed duly
		/// allocated that, so the right 300px of every page hung off the window (measured: an 820px
		/// window allocating the stack 820px starting at x=300). The renderer always gives this
		/// widget an explicit request equal to the page area, so honour it and let the box
		/// distribute what is actually there.
		/// </remarks>
		protected override void OnGetPreferredWidth(out int minimum_width, out int natural_width)
		{
			base.OnGetPreferredWidth(out minimum_width, out natural_width);

			if (WidthRequest > 0)
				minimum_width = natural_width = WidthRequest;
		}

		protected override void OnGetPreferredHeight(out int minimum_height, out int natural_height)
		{
			base.OnGetPreferredHeight(out minimum_height, out natural_height);

			if (HeightRequest > 0)
				minimum_height = natural_height = HeightRequest;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			// Never measure or resize from inside a size-allocate: GTK3 discards the resizes that
			// queues, and Forms measurement is exactly what the flyout layout has to do.
			if (_layoutQueued)
				return;

			_layoutQueued = true;

			GLib.Idle.Add(() =>
			{
				_layoutQueued = false;
				LayoutRefreshed?.Invoke(this, EventArgs.Empty);
				return false;
			});
		}

		bool _layoutQueued;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_toggleBox.ButtonPressEvent -= OnTogglePressed;
				_backBox.ButtonPressEvent -= OnBackPressed;
			}

			base.Dispose(disposing);
		}

		void OnTogglePressed(object o, ButtonPressEventArgs args)
		{
			if (_behavior != ShellFlyoutBehaviorType.Flyout)
				return;

			IsPresented = !IsPresented;
			IsPresentedChanged?.Invoke(this, EventArgs.Empty);

			args.RetVal = true;
		}

		void OnBackPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			BackRequested?.Invoke(this, EventArgs.Empty);
		}

		void RefreshTabBars()
		{
			if (_sectionTabsVisible)
				_sectionTabs.Show();
			else
				_sectionTabs.Hide();

			if (_contentTabsVisible)
				_contentTabs.Show();
			else
				_contentTabs.Hide();
		}

		void RefreshBehavior()
		{
			// Locked keeps the flyout permanently beside the content, so the content is inset by
			// it. Everything else overlays, so the content spans the full width.
			_contentStack.MarginStart = _behavior == ShellFlyoutBehaviorType.Locked ? _flyoutWidth : 0;

			RefreshFlyoutVisibility();
			RefreshNavBarVisibility();
		}

		void RefreshFlyoutVisibility()
		{
			if (FlyoutVisible)
			{
				// gtk_widget_show_all skips a widget with no_show_all set, so ShowAll on the wrapper
				// would be a no-op: show the contents through the inner box and the wrapper itself
				// with the plain Show(), which does not consult the flag.
				_flyoutBox.ShowAll();
				_flyoutWrapper.Show();
				_flyoutWrapper.Window?.Raise();
			}
			else
			{
				_flyoutWrapper.Hide();
			}
		}

		void RefreshNavBarVisibility()
		{
			if (_navBarVisible)
			{
				_navBarBox.ShowAll();
				_navBar.Show();
			}
			else
			{
				_navBar.Hide();
			}

			// The back arrow replaces the hamburger while there is somewhere to go back to; both
			// set NoShowAll so a stray ShowAll cannot resurrect the one that should be hidden -
			// which also means the ShowAll above skipped the labels inside them.
			if (_backVisible)
			{
				_backLabel.Show();
				_backBox.Show();
				_toggleBox.Hide();

				return;
			}

			_backBox.Hide();

			// Only a Flyout-behaviour shell can be toggled: Locked is always open and Disabled has
			// no flyout at all, so in both cases the hamburger would be a dead control.
			if (_behavior == ShellFlyoutBehaviorType.Flyout)
			{
				_toggleLabel.Show();
				_toggleBox.Show();
			}
			else
			{
				_toggleBox.Hide();
			}
		}
	}
}
