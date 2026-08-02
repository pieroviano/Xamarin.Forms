using System;
using System.Collections.Generic;
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
	/// How the nav bar's search box is presented, mirroring
	/// <see cref="Xamarin.Forms.SearchBoxVisibility"/>.
	/// </summary>
	public enum ShellSearchBoxMode
	{
		Hidden = 0,
		Collapsible,
		Expanded
	}

	/// <summary>
	/// One nav bar toolbar entry, flattened out of a <see cref="Xamarin.Forms.ToolbarItem"/> so the
	/// widget needs no Forms knowledge.
	/// </summary>
	public sealed class ShellToolbarItemInfo
	{
		public string Text { get; set; }

		public Gdk.Pixbuf Icon { get; set; }

		public bool IsEnabled { get; set; } = true;

		/// <summary>Secondary items live behind the overflow button, not on the bar.</summary>
		public bool IsSecondary { get; set; }
	}

	/// <summary>Carries the index, into the list last handed to
	/// <c>ShellWidget.SetToolbarItems</c>, of the entry the user activated.</summary>
	public class ShellToolbarItemActivatedEventArgs : EventArgs
	{
		public ShellToolbarItemActivatedEventArgs(int index)
		{
			Index = index;
		}

		public int Index { get; }
	}

	/// <summary>Carries the index of the search suggestion row the user picked.</summary>
	public class ShellSuggestionSelectedEventArgs : EventArgs
	{
		public ShellSuggestionSelectedEventArgs(int index)
		{
			Index = index;
		}

		public int Index { get; }
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
		public const int SearchBoxWidth = 220;
		public const int SearchBoxHeight = 32;
		public const int MaxSuggestionsHeight = 260;

		sealed class ToolbarEntry
		{
			public int Index;
			public EventBox Host;
			public Gtk.Label Label;
		}

		readonly EventBox _navBar;
		readonly Gtk.Box _navBarBox;
		readonly EventBox _toggleBox;
		readonly Gtk.Label _toggleLabel;
		readonly EventBox _backBox;
		readonly Gtk.Label _backLabel;
		readonly Gtk.Label _titleLabel;
		readonly EventBox _titleViewHost;

		readonly Gtk.Box _searchHost;
		readonly EventBox _searchToggleBox;
		readonly Gtk.Label _searchToggleLabel;
		readonly Gtk.Box _searchEntryHost;
		readonly SearchEntry _searchEntry;
		readonly EventBox _clearPlaceholderBox;
		readonly Gtk.Label _clearPlaceholderLabel;

		readonly Gtk.Box _toolbarBox;
		readonly Gtk.Box _toolbarItemsBox;
		readonly EventBox _overflowBox;
		readonly Gtk.Label _overflowLabel;
		readonly Gtk.Menu _overflowMenu;
		readonly List<ToolbarEntry> _toolbarEntries = new List<ToolbarEntry>();
		readonly Dictionary<Gtk.MenuItem, int> _overflowEntries = new Dictionary<Gtk.MenuItem, int>();

		readonly ShellTabBar _sectionTabs;
		readonly ShellTabBar _contentTabs;

		readonly Gtk.Overlay _body;
		readonly Gtk.Stack _contentStack;

		readonly EventBox _suggestionsWrapper;
		readonly Gtk.ScrolledWindow _suggestionsScroller;
		readonly Gtk.Viewport _suggestionsViewport;
		readonly Gtk.Box _suggestionsBox;

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
		bool _titleViewVisible;
		bool _searchExpanded;
		bool _clearPlaceholderVisible;
		bool _suggestionsVisible;
		ShellSearchBoxMode _searchMode = ShellSearchBoxMode.Hidden;
		int _flyoutWidth = DefaultFlyoutWidth;
		Gdk.Color? _navForeground;
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

			// Shell.TitleView takes the title label's slot rather than sitting beside it, which is
			// what every other backend does. Expand/fill, so the box hands it whatever the leading
			// affordances, the search box and the toolbar leave over - that allocation is then the
			// width the renderer measures the Forms view against.
			_titleViewHost = new EventBox { VisibleWindow = false, NoShowAll = true };
			_titleViewHost.HeightRequest = NavBarHeight;

			// ---- search box ---------------------------------------------------------------
			// The native control is SearchBarRenderer's own SearchEntry, not a second
			// implementation of the same thing: it already carries the query icon, the clear
			// button, the placeholder and the text/placeholder/font setters.
			_searchEntry = new SearchEntry
			{
				WidthRequest = SearchBoxWidth,
				HeightRequest = SearchBoxHeight,
				Valign = Align.Center
			};

			_clearPlaceholderLabel = new Gtk.Label("✕");
			_clearPlaceholderBox = new EventBox { VisibleWindow = false, NoShowAll = true, WidthRequest = 28 };
			_clearPlaceholderBox.Add(_clearPlaceholderLabel);
			_clearPlaceholderBox.ButtonPressEvent += OnClearPlaceholderPressed;

			_searchEntryHost = new Gtk.Box(Gtk.Orientation.Horizontal, 0) { NoShowAll = true };
			_searchEntryHost.PackStart(_searchEntry, false, false, 0);
			_searchEntryHost.PackStart(_clearPlaceholderBox, false, false, 2);

			_searchToggleLabel = new Gtk.Label("🔍");
			_searchToggleBox = new EventBox { VisibleWindow = false, NoShowAll = true, WidthRequest = 28 };
			_searchToggleBox.Add(_searchToggleLabel);
			_searchToggleBox.ButtonPressEvent += OnSearchTogglePressed;

			_searchHost = new Gtk.Box(Gtk.Orientation.Horizontal, 0) { NoShowAll = true, Valign = Align.Center };
			_searchHost.PackStart(_searchToggleBox, false, false, 0);
			_searchHost.PackStart(_searchEntryHost, false, false, 0);

			// ---- toolbar items ------------------------------------------------------------
			_toolbarItemsBox = new Gtk.Box(Gtk.Orientation.Horizontal, 0);

			_overflowLabel = new Gtk.Label("⋮");
			_overflowBox = new EventBox { VisibleWindow = false, NoShowAll = true, WidthRequest = 24 };
			_overflowBox.Add(_overflowLabel);
			_overflowBox.ButtonPressEvent += OnOverflowPressed;

			_overflowMenu = new Gtk.Menu();

			// NoShowAll on the wrapper, so a nav bar ShowAll() cannot resurrect a toolbar that has no
			// entries; the entries themselves are shown through _toolbarItemsBox.
			_toolbarBox = new Gtk.Box(Gtk.Orientation.Horizontal, 0) { NoShowAll = true, Valign = Align.Center };
			_toolbarBox.PackStart(_toolbarItemsBox, false, false, 0);
			_toolbarBox.PackStart(_overflowBox, false, false, 0);

			_navBarBox.PackStart(_backBox, false, false, 10);
			_navBarBox.PackStart(_toggleBox, false, false, 10);
			_navBarBox.PackStart(_titleLabel, false, false, 6);
			_navBarBox.PackStart(_titleViewHost, true, true, 6);

			// PackEnd fills from the right, so the first call is the rightmost widget: the toolbar
			// sits at the far end and the search box just inside it.
			_navBarBox.PackEnd(_toolbarBox, false, false, 6);
			_navBarBox.PackEnd(_searchHost, false, false, 6);

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

			// The search suggestion list is an overlay child, added BEFORE the flyout so the flyout
			// still paints above it, and pinned to the top of the body - i.e. directly under the
			// nav bar / tab strips. A popup Gtk.Window would grab the pointer, which is both wrong
			// over a Forms page and untestable headlessly.
			_suggestionsBox = new Gtk.Box(Gtk.Orientation.Vertical, 0);

			_suggestionsViewport = new Gtk.Viewport { ShadowType = ShadowType.None };
			_suggestionsViewport.Add(_suggestionsBox);

			_suggestionsScroller = new Gtk.ScrolledWindow();
			_suggestionsScroller.SetPolicy(PolicyType.Never, PolicyType.Automatic);
			_suggestionsScroller.Add(_suggestionsViewport);

			_suggestionsWrapper = new EventBox
			{
				VisibleWindow = true,
				NoShowAll = true,
				Halign = Align.Fill,
				Valign = Align.Start
			};

			_suggestionsWrapper.Add(_suggestionsScroller);
			_body.AddOverlay(_suggestionsWrapper);

			ApplySuggestionsBackground();

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
			RefreshTitleView();
			RefreshSearchBox();
			RefreshToolbar();
			RefreshSuggestions();
		}

		/// <summary>Raised when the user toggles the flyout from the nav bar.</summary>
		public event EventHandler IsPresentedChanged;

		/// <summary>Raised when the user activates a nav bar toolbar entry (primary or overflow).</summary>
		public event EventHandler<ShellToolbarItemActivatedEventArgs> ToolbarItemActivated;

		/// <summary>Raised when the user clicks a search suggestion row.</summary>
		public event EventHandler<ShellSuggestionSelectedEventArgs> SuggestionSelected;

		/// <summary>Raised when the collapsible search box is expanded or collapsed by its button.</summary>
		public event EventHandler SearchExpandedChanged;

		/// <summary>Raised when the user clicks the "clear placeholder" affordance.</summary>
		public event EventHandler ClearPlaceholderClicked;

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

		/// <summary>The nav bar host the <c>Shell.TitleView</c>'s renderer is put into.</summary>
		public EventBox TitleViewHost => _titleViewHost;

		/// <summary>The nav bar's title label - hidden while a <c>TitleView</c> is shown.</summary>
		public Gtk.Label TitleLabel => _titleLabel;

		/// <summary>The nav bar search box, i.e. <c>SearchBarRenderer</c>'s own native control.</summary>
		public SearchEntry SearchEntry => _searchEntry;

		/// <summary>The wrapper holding the search toggle and the search box.</summary>
		public Gtk.Box SearchHost => _searchHost;

		/// <summary>The magnifier that expands a <c>Collapsible</c> search box.</summary>
		public EventBox SearchToggle => _searchToggleBox;

		/// <summary>The <c>ClearPlaceholder</c> affordance beside the search box.</summary>
		public EventBox ClearPlaceholderButton => _clearPlaceholderBox;

		/// <summary>The vertical box the search suggestion rows are packed into.</summary>
		public Gtk.Box SuggestionsBox => _suggestionsBox;

		/// <summary>The suggestion list's wrapper - the widget that is shown, hidden and sized.</summary>
		public EventBox SuggestionsWrapper => _suggestionsWrapper;

		/// <summary>The primary toolbar entries' box; the overflow button is its sibling.</summary>
		public Gtk.Box ToolbarItemsBox => _toolbarItemsBox;

		/// <summary>The "⋮" button that pops the secondary toolbar entries.</summary>
		public EventBox ToolbarOverflow => _overflowBox;

		/// <summary>The menu the secondary toolbar entries live in.</summary>
		public Gtk.Menu ToolbarOverflowMenu => _overflowMenu;

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

		/// <summary>Whether a <c>Shell.TitleView</c> replaces the nav bar's title label.</summary>
		public bool TitleViewVisible
		{
			get => _titleViewVisible;
			set
			{
				if (_titleViewVisible == value)
					return;

				_titleViewVisible = value;
				RefreshTitleView();
			}
		}

		/// <summary>How - and whether - the search box is presented.</summary>
		public ShellSearchBoxMode SearchMode
		{
			get => _searchMode;
			set
			{
				if (_searchMode == value)
					return;

				_searchMode = value;

				// A box that stops being collapsible must not stay stuck in whichever state its
				// toggle happened to leave it in.
				if (_searchMode != ShellSearchBoxMode.Collapsible)
					_searchExpanded = false;

				RefreshSearchBox();
			}
		}

		/// <summary>Whether a <see cref="ShellSearchBoxMode.Collapsible"/> box is currently open.</summary>
		public bool SearchExpanded
		{
			get => _searchMode == ShellSearchBoxMode.Expanded || _searchExpanded;
			set
			{
				if (_searchExpanded == value)
					return;

				_searchExpanded = value;
				RefreshSearchBox();
			}
		}

		/// <summary>Whether the <c>ClearPlaceholder</c> affordance is offered.</summary>
		public bool ClearPlaceholderVisible
		{
			get => _clearPlaceholderVisible;
			set
			{
				if (_clearPlaceholderVisible == value)
					return;

				_clearPlaceholderVisible = value;
				RefreshSearchBox();
			}
		}

		/// <summary>Whether the search suggestion list is on screen.</summary>
		public bool SuggestionsVisible
		{
			get => _suggestionsVisible;
			set
			{
				if (_suggestionsVisible == value)
					return;

				_suggestionsVisible = value;
				RefreshSuggestions();
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
			_navForeground = foreground;

			// Color.Default means "let the GTK theme draw it", so the reset is ClearStyle().
			if (background.HasValue)
				_navBar.SetBackgroundColor(background.Value);
			else
				_navBar.ClearStyle();

			ApplyNavBarForeground(_titleLabel);
			ApplyNavBarForeground(_toggleLabel);
			ApplyNavBarForeground(_backLabel);
			ApplyNavBarForeground(_searchToggleLabel);
			ApplyNavBarForeground(_clearPlaceholderLabel);
			ApplyNavBarForeground(_overflowLabel);

			foreach (var entry in _toolbarEntries)
				ApplyNavBarForeground(entry.Label);
		}

		void ApplyNavBarForeground(Gtk.Label label)
		{
			if (label == null)
				return;

			if (_navForeground.HasValue)
				label.SetForegroundColor(_navForeground.Value);
			else
				label.ClearStyle();
		}

		/// <summary>
		/// Replaces the nav bar's toolbar entries: primary ones as buttons on the bar, secondary
		/// ones behind the "⋮" overflow.
		/// </summary>
		/// <remarks>
		/// The entries are <see cref="EventBox"/> + <see cref="Gtk.Label"/>, never
		/// <see cref="Gtk.Button"/>: a themed GTK3 button paints a <c>background-image</c> gradient
		/// over whatever colour is set on it, so a toolbar entry on a <c>Shell.BackgroundColor</c>
		/// nav bar would render theme-grey. Same rule as the hamburger and the tabs.
		///
		/// A disabled entry is <c>Sensitive = false</c> - which is what makes GTK grey it - and its
		/// press handler returns without raising anything, because an insensitive
		/// <c>EventBox</c> still owns no input window when <c>VisibleWindow</c> is false and a
		/// synthesized event can therefore still reach it.
		/// </remarks>
		public void SetToolbarItems(IList<ShellToolbarItemInfo> items)
		{
			foreach (var entry in _toolbarEntries)
			{
				entry.Host.ButtonPressEvent -= OnToolbarItemPressed;
				_toolbarItemsBox.Remove(entry.Host);
				entry.Host.Destroy();
			}

			_toolbarEntries.Clear();

			foreach (var pair in _overflowEntries)
			{
				pair.Key.Activated -= OnOverflowItemActivated;
				_overflowMenu.Remove(pair.Key);
				pair.Key.Destroy();
			}

			_overflowEntries.Clear();

			if (items != null)
			{
				for (var i = 0; i < items.Count; i++)
				{
					var item = items[i];

					if (item == null)
						continue;

					if (item.IsSecondary)
					{
						var menuItem = new Gtk.MenuItem(item.Text ?? string.Empty) { Sensitive = item.IsEnabled };

						menuItem.Activated += OnOverflowItemActivated;
						_overflowMenu.Add(menuItem);
						_overflowEntries[menuItem] = i;

						continue;
					}

					var label = new Gtk.Label(item.Text ?? string.Empty);
					var column = new Gtk.Box(Gtk.Orientation.Horizontal, 4);

					if (item.Icon != null)
						column.PackStart(new Gtk.Image(item.Icon), false, false, 0);

					column.PackStart(label, false, false, 0);

					var host = new EventBox { VisibleWindow = false, Sensitive = item.IsEnabled };
					host.Add(column);
					host.ButtonPressEvent += OnToolbarItemPressed;

					_toolbarItemsBox.PackStart(host, false, false, 6);
					_toolbarEntries.Add(new ToolbarEntry { Index = i, Host = host, Label = label });

					ApplyNavBarForeground(label);
				}
			}

			// Show the menu ITEMS, never the Gtk.Menu itself - gtk_widget_show on a GtkMenu maps
			// its toplevel, i.e. it pops the menu open. The menu is shown in the popup handler.
			foreach (var child in _overflowMenu.Children)
				child.ShowAll();

			RefreshToolbar();
		}

		void RefreshToolbar()
		{
			_toolbarItemsBox.ShowAll();

			if (_overflowEntries.Count > 0)
			{
				_overflowLabel.Show();
				_overflowBox.Show();
			}
			else
			{
				_overflowBox.Hide();
			}

			if (_toolbarEntries.Count > 0 || _overflowEntries.Count > 0)
				_toolbarBox.Show();
			else
				_toolbarBox.Hide();
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

		/// <summary>
		/// Replaces the search suggestion rows. The caller owns the row widgets; this only re-parents
		/// them and wires the click that reports the row index back.
		/// </summary>
		public void SetSuggestionRows(IList<Gtk.Widget> rows)
		{
			foreach (var child in _suggestionsBox.Children)
			{
				if (child is EventBox box)
					box.ButtonPressEvent -= OnSuggestionPressed;

				_suggestionsBox.Remove(child);
			}

			_suggestionRows.Clear();

			if (rows != null)
			{
				foreach (var row in rows)
				{
					if (row == null)
						continue;

					if (row is EventBox box)
						box.ButtonPressEvent += OnSuggestionPressed;

					_suggestionsBox.PackStart(row, false, false, 0);
					_suggestionRows.Add(row);
				}
			}

			_suggestionsBox.ShowAll();
			RefreshSuggestions();
		}

		readonly List<Gtk.Widget> _suggestionRows = new List<Gtk.Widget>();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_toggleBox.ButtonPressEvent -= OnTogglePressed;
				_backBox.ButtonPressEvent -= OnBackPressed;
				_searchToggleBox.ButtonPressEvent -= OnSearchTogglePressed;
				_clearPlaceholderBox.ButtonPressEvent -= OnClearPlaceholderPressed;
				_overflowBox.ButtonPressEvent -= OnOverflowPressed;

				foreach (var entry in _toolbarEntries)
					entry.Host.ButtonPressEvent -= OnToolbarItemPressed;

				foreach (var pair in _overflowEntries)
					pair.Key.Activated -= OnOverflowItemActivated;

				foreach (var row in _suggestionRows)
				{
					if (row is EventBox box)
						box.ButtonPressEvent -= OnSuggestionPressed;
				}
			}

			base.Dispose(disposing);
		}

		void OnToolbarItemPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			foreach (var entry in _toolbarEntries)
			{
				if (!ReferenceEquals(entry.Host, o))
					continue;

				// An insensitive EventBox with no input window still receives a synthesized event,
				// so "disabled" has to be enforced here and not left to GTK's grab handling.
				if (!entry.Host.Sensitive)
					return;

				ToolbarItemActivated?.Invoke(this, new ShellToolbarItemActivatedEventArgs(entry.Index));
				return;
			}
		}

		void OnOverflowItemActivated(object sender, EventArgs e)
		{
			if (sender is Gtk.MenuItem menuItem && _overflowEntries.TryGetValue(menuItem, out var index))
				ToolbarItemActivated?.Invoke(this, new ShellToolbarItemActivatedEventArgs(index));
		}

		void OnOverflowPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			if (_overflowEntries.Count == 0)
				return;

			_overflowMenu.ShowAll();
			_overflowMenu.Popup();
		}

		void OnSearchTogglePressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			if (_searchMode != ShellSearchBoxMode.Collapsible)
				return;

			_searchExpanded = !_searchExpanded;
			RefreshSearchBox();

			SearchExpandedChanged?.Invoke(this, EventArgs.Empty);
		}

		void OnClearPlaceholderPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			ClearPlaceholderClicked?.Invoke(this, EventArgs.Empty);
		}

		void OnSuggestionPressed(object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;

			var index = _suggestionRows.IndexOf(o as Gtk.Widget);

			if (index >= 0)
				SuggestionSelected?.Invoke(this, new ShellSuggestionSelectedEventArgs(index));
		}

		void RefreshTitleView()
		{
			if (_titleViewVisible)
			{
				_titleLabel.Hide();
				_titleViewHost.Show();
			}
			else
			{
				_titleViewHost.Hide();

				if (_navBarVisible)
					_titleLabel.Show();
			}
		}

		void RefreshSearchBox()
		{
			if (_searchMode == ShellSearchBoxMode.Hidden)
			{
				_searchHost.Hide();
				return;
			}

			if (SearchExpanded)
			{
				_searchToggleBox.Hide();
				_searchEntry.ShowAll();

				if (_clearPlaceholderVisible)
				{
					_clearPlaceholderLabel.Show();
					_clearPlaceholderBox.Show();
				}
				else
				{
					_clearPlaceholderBox.Hide();
				}

				_searchEntryHost.Show();
			}
			else
			{
				_searchEntryHost.Hide();
				_searchToggleLabel.Show();
				_searchToggleBox.Show();
			}

			_searchHost.Show();
		}

		/// <summary>
		/// Paints the suggestion overlay opaque.
		/// </summary>
		/// <remarks>
		/// An overlay child draws over the page but does not get a background of its own: a
		/// <see cref="EventBox"/> with <c>VisibleWindow = true</c> still renders nothing but what CSS
		/// gives it. Left unstyled the page shows straight through the suggestion rows and the two
		/// texts collide - measured on the first run of <c>shell4-smoke.sh</c>, where the page's
		/// "UNREAD MESSAGES" heading was painted between "beta" and "gamma". Every geometry assertion
		/// in that suite passed while this was happening, which is why it took a screenshot to find.
		///
		/// All three layers are styled for the reason the flyout has to do the same (see
		/// <see cref="UpdateFlyoutBackgroundColor"/>): a CssProvider added to a widget's own
		/// StyleContext styles that widget only, so colouring the wrapper alone leaves the
		/// ScrolledWindow and the Viewport inside it painting the theme's background over the top.
		/// </remarks>
		void ApplySuggestionsBackground()
		{
			var background = LookupThemeColor("theme_base_color")
				?? LookupThemeColor("theme_bg_color")
				?? new Gdk.Color(0xff, 0xff, 0xff);

			_suggestionsWrapper.SetBackgroundColor(background);
			_suggestionsScroller.SetBackgroundColor(background);
			_suggestionsViewport.SetBackgroundColor(background);
		}

		Gdk.Color? LookupThemeColor(string name)
		{
			if (!StyleContext.LookupColor(name, out var rgba))
				return null;

			// An unresolvable or fully transparent theme colour is worse than no colour at all here,
			// because the whole point is opacity.
			if (rgba.Alpha <= 0.0)
				return null;

			return new Gdk.Color(
				(byte)(rgba.Red * 255.0),
				(byte)(rgba.Green * 255.0),
				(byte)(rgba.Blue * 255.0));
		}

		void RefreshSuggestions()
		{
			if (_suggestionsVisible && _suggestionRows.Count > 0)
			{
				_suggestionsScroller.ShowAll();
				_suggestionsWrapper.Show();
				_suggestionsWrapper.Window?.Raise();
			}
			else
			{
				_suggestionsWrapper.Hide();
			}
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

				// ShowAll has just un-hidden the title label and every toolbar entry regardless of
				// whether a TitleView or a collapsed search box wants them; put that back.
				RefreshNavBarContents();
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

		/// <summary>
		/// Re-applies the state of everything in the nav bar that <c>ShowAll()</c> would have
		/// trampled, and that <see cref="RefreshNavBarVisibility"/> therefore has to restore.
		/// </summary>
		void RefreshNavBarContents()
		{
			RefreshTitleView();
			RefreshSearchBox();
			RefreshToolbar();
		}
	}
}
