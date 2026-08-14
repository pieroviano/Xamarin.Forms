using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;
using FormsPage = Xamarin.Forms.Page;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// Shell on GTK: the flyout (header, items, menu items, footer), the current page, the
	/// <see cref="ShellSection"/>/<see cref="ShellContent"/> tab bars, and the navigation stack
	/// driven by <c>GoToAsync</c>, <c>PushAsync</c>/<c>PopAsync</c> and the nav bar's back arrow.
	/// </summary>
	/// <remarks>
	/// Scope: stages one to three of the plan, plus stage four's nav bar surface - page/Shell
	/// <see cref="ToolbarItem"/>s (primary on the bar, secondary behind an overflow),
	/// <c>Shell.TitleView</c> and the <see cref="SearchHandler"/> search box with its suggestion
	/// list. Still <b>not</b> rendered are modal presentation and the flyout transition; see the
	/// plan's "not implemented" table.
	///
	/// Structure it relies on (identical to the UWP and Android renderers):
	/// <code>
	/// Shell -> ShellItem -> ShellSection -> ShellContent -> Page
	/// </code>
	/// Content changes arrive on exactly two paths and no others:
	/// <see cref="IShellSectionController.AddDisplayedPageObserver"/> for a <c>ShellContent</c>
	/// switch, and <see cref="IShellSectionController.NavigationRequested"/> for a nav-stack
	/// push/pop/insert/remove. Core never updates <c>DisplayedPage</c> for a push, which is why
	/// both are needed - subscribing to only the first is the bug stage three had to fix.
	///
	/// Flyout item cells are ordinary Forms views built from
	/// <c>IShellController.GetFlyoutItemDataTemplate</c> - so <c>Shell.ItemTemplate</c> and
	/// <c>Shell.MenuItemTemplate</c> work - which means the renderer must parent, measure and lay
	/// them out itself: they are not children of a Forms <c>Layout</c>, so nothing else will.
	/// </remarks>
	public class ShellRenderer : AbstractPageRenderer<ShellWidget, Shell>, IFlyoutBehaviorObserver, IAppearanceObserver
	{
		sealed class FlyoutHost
		{
			public Element Element;
			public View View;
			public IVisualElementRenderer Renderer;
			public Gtk.EventBox Host;

			/// <summary>
			/// Whether this renderer is the one that parented <see cref="View"/>, and may therefore
			/// un-parent it again. A <c>Shell.TitleView</c> is parented by Core to the page that
			/// declares it; clearing that would cut the view off from its own binding context.
			/// </summary>
			public bool OwnsParent = true;
		}

		readonly List<FlyoutHost> _flyoutHosts = new List<FlyoutHost>();
		readonly List<FlyoutHost> _suggestionHosts = new List<FlyoutHost>();
		readonly List<Gtk.Widget> _separators = new List<Gtk.Widget>();
		readonly List<ToolbarItem> _toolbarItems = new List<ToolbarItem>();
		readonly Dictionary<FormsPage, IVisualElementRenderer> _pageRenderers =
			new Dictionary<FormsPage, IVisualElementRenderer>();

		ShellItem _currentShellItem;
		ShellSection _currentSection;
		FormsPage _currentPage;

		FlyoutHost _headerHost;
		FlyoutHost _footerHost;
		FlyoutHost _titleViewHost;

		FormsPage _toolbarPage;
		SearchHandler _searchHandler;
		INotifyCollectionChanged _searchItemsSource;

		FlyoutBehavior _behavior = FlyoutBehavior.Flyout;
		bool _updatingPresented;
		bool _updatingQuery;
		int _pageId;

		Shell ShellElement => Element as Shell;

		IShellController ShellController => Element as IShellController;

		// ---- element wiring --------------------------------------------------------------

		protected override void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);

			if (e.NewElement == null || Widget != null)
				return;

			Widget = new ShellWidget();

			var container = new GtkFormsContainer();
			container.Add(Widget);
			Control.Content = container;

			Widget.IsPresentedChanged += OnWidgetIsPresentedChanged;
			Widget.LayoutRefreshed += OnWidgetLayoutRefreshed;
			Widget.BackRequested += OnBackRequested;
			Widget.SectionTabs.TabSelected += OnSectionTabSelected;
			Widget.ContentTabs.TabSelected += OnContentTabSelected;
			Widget.ToolbarItemActivated += OnToolbarItemActivated;
			Widget.SuggestionSelected += OnSuggestionSelected;
			Widget.ClearPlaceholderClicked += OnClearPlaceholderClicked;
			Widget.SearchExpandedChanged += OnSearchExpandedChanged;
			Widget.SearchEntry.SearchTextChanged += OnSearchTextChanged;
			Widget.SearchEntry.SearchButtonClicked += OnSearchConfirmed;
			Widget.SearchEntry.Entry.Activated += OnSearchConfirmed;

			ShellController.StructureChanged += OnStructureChanged;
			ShellController.FlyoutItemsChanged += OnFlyoutItemsChanged;
			ShellController.AddFlyoutBehaviorObserver(this);
			ShellController.AddAppearanceObserver(this, Element);

			UpdateFlyoutBehavior();
			UpdateFlyoutBackgroundColor();
			UpdateFlyoutHeader();
			UpdateFlyoutFooter();
			RebuildFlyoutItems();
			UpdateCurrentItem();
			UpdateIsPresented();
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == Shell.CurrentItemProperty.PropertyName)
				UpdateCurrentItem();
			else if (e.PropertyName == Shell.FlyoutIsPresentedProperty.PropertyName)
				UpdateIsPresented();
			else if (e.PropertyName == Shell.FlyoutHeaderProperty.PropertyName ||
				e.PropertyName == Shell.FlyoutHeaderTemplateProperty.PropertyName)
				UpdateFlyoutHeader();
			else if (e.PropertyName == Shell.FlyoutFooterProperty.PropertyName ||
				e.PropertyName == Shell.FlyoutFooterTemplateProperty.PropertyName)
				UpdateFlyoutFooter();
			else if (e.PropertyName == Shell.FlyoutBackgroundColorProperty.PropertyName)
				UpdateFlyoutBackgroundColor();
			else if (e.PropertyName == Shell.FlyoutBehaviorProperty.PropertyName)
				UpdateFlyoutBehavior();
			else if (e.PropertyName == Shell.ItemTemplateProperty.PropertyName ||
				e.PropertyName == Shell.MenuItemTemplateProperty.PropertyName)
				RebuildFlyoutItems();
			else if (e.PropertyName == Shell.TitleViewProperty.PropertyName)
				UpdateTitleView();
			else if (e.PropertyName == Shell.SearchHandlerProperty.PropertyName)
				UpdateSearchHandler();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			Control?.Content?.SetSize(allocation.Width, allocation.Height);

			// The shell widget needs the request too, not just the container around it: it is what
			// caps the natural size the enclosing Gtk.Fixed would otherwise allocate. SetSize is a
			// no-op when the value has not changed, which is what keeps this safe to call from
			// inside a size-allocate.
			Widget?.SetSize(allocation.Width, allocation.Height);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Widget != null)
				{
					Widget.IsPresentedChanged -= OnWidgetIsPresentedChanged;
					Widget.LayoutRefreshed -= OnWidgetLayoutRefreshed;
					Widget.BackRequested -= OnBackRequested;
					Widget.SectionTabs.TabSelected -= OnSectionTabSelected;
					Widget.ContentTabs.TabSelected -= OnContentTabSelected;
					Widget.ToolbarItemActivated -= OnToolbarItemActivated;
					Widget.SuggestionSelected -= OnSuggestionSelected;
					Widget.ClearPlaceholderClicked -= OnClearPlaceholderClicked;
					Widget.SearchExpandedChanged -= OnSearchExpandedChanged;
					Widget.SearchEntry.SearchTextChanged -= OnSearchTextChanged;
					Widget.SearchEntry.SearchButtonClicked -= OnSearchConfirmed;
					Widget.SearchEntry.Entry.Activated -= OnSearchConfirmed;
				}

				if (ShellController != null)
				{
					ShellController.StructureChanged -= OnStructureChanged;
					ShellController.FlyoutItemsChanged -= OnFlyoutItemsChanged;
					ShellController.RemoveFlyoutBehaviorObserver(this);
					ShellController.RemoveAppearanceObserver(this);
				}

				if (_currentPage != null)
					_currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;

				DetachToolbarPage();
				DetachSearchHandler();
				DetachShellItem();
				ClearFlyoutItems();
				ClearSuggestions();
				DestroyFlyoutHost(ref _headerHost);
				DestroyFlyoutHost(ref _footerHost);
				DestroyFlyoutHost(ref _titleViewHost);

				foreach (var pair in _pageRenderers.ToList())
				{
					Detach(pair.Value.Container);
					pair.Value.Dispose();
					Platform.SetRenderer(pair.Key, null);
				}

				_pageRenderers.Clear();
				_currentPage = null;
			}

			base.Dispose(disposing);
		}

		// ---- flyout behaviour / appearance -----------------------------------------------

		void IFlyoutBehaviorObserver.OnFlyoutBehaviorChanged(FlyoutBehavior behavior)
		{
			_behavior = behavior;
			ApplyFlyoutBehavior();
		}

		void IAppearanceObserver.OnAppearanceChanged(ShellAppearance appearance)
		{
			if (Widget == null)
				return;

			// A null appearance means nothing along the pivot line set one, i.e. "use the theme".
			var background = appearance == null || appearance.BackgroundColor.IsDefaultOrTransparent()
				? (Gdk.Color?)null
				: appearance.BackgroundColor.ToGtkColor();

			var foreground = appearance == null || appearance.TitleColor.IsDefaultOrTransparent()
				? (appearance == null || appearance.ForegroundColor.IsDefaultOrTransparent()
					? (Gdk.Color?)null
					: appearance.ForegroundColor.ToGtkColor())
				: appearance.TitleColor.ToGtkColor();

			Widget.UpdateNavBarColors(background, foreground);

			// The tab bars have their own colour family, and Core already folds each one back onto
			// its non-tab equivalent through IShellAppearanceElement's Effective* properties, so a
			// Shell that only sets BackgroundColor still gets a matching tab bar.
			var element = appearance as IShellAppearanceElement;

			Widget.UpdateTabBarColors(
				ToGtk(element?.EffectiveTabBarBackgroundColor ?? Color.Default),
				ToGtk(element?.EffectiveTabBarTitleColor ?? Color.Default) ??
					ToGtk(element?.EffectiveTabBarForegroundColor ?? Color.Default),
				ToGtk(element?.EffectiveTabBarUnselectedColor ?? Color.Default));

			if (appearance != null && appearance.FlyoutWidth > 0)
				Widget.FlyoutWidth = (int)appearance.FlyoutWidth;
		}

		static Gdk.Color? ToGtk(Color color) =>
			color.IsDefaultOrTransparent() ? (Gdk.Color?)null : color.ToGtkColor();

		void UpdateFlyoutBehavior()
		{
			if (ShellElement == null)
				return;

			_behavior = ShellElement.FlyoutBehavior;
			ApplyFlyoutBehavior();
		}

		void ApplyFlyoutBehavior()
		{
			if (Widget == null)
				return;

			switch (_behavior)
			{
				case FlyoutBehavior.Disabled:
					Widget.FlyoutBehaviorType = ShellFlyoutBehaviorType.Disabled;
					break;
				case FlyoutBehavior.Locked:
					Widget.FlyoutBehaviorType = ShellFlyoutBehaviorType.Locked;
					break;
				default:
					Widget.FlyoutBehaviorType = ShellFlyoutBehaviorType.Flyout;
					break;
			}

			QueueFlyoutLayout();
		}

		void UpdateFlyoutBackgroundColor()
		{
			var color = ShellElement?.FlyoutBackgroundColor ?? Color.Default;

			Widget?.UpdateFlyoutBackgroundColor(color.IsDefaultOrTransparent() ? (Gdk.Color?)null : color.ToGtkColor());
		}

		void UpdateIsPresented()
		{
			if (Widget == null || ShellElement == null)
				return;

			_updatingPresented = true;

			try
			{
				Widget.IsPresented = ShellElement.FlyoutIsPresented;
			}
			finally
			{
				_updatingPresented = false;
			}

			QueueFlyoutLayout();
		}

		void OnWidgetIsPresentedChanged(object sender, EventArgs e)
		{
			if (_updatingPresented || ShellElement == null)
				return;

			ElementController.SetValueFromRenderer(Shell.FlyoutIsPresentedProperty, Widget.IsPresented);

			QueueFlyoutLayout();
		}

		// ---- current item / content ------------------------------------------------------

		void OnStructureChanged(object sender, EventArgs e)
		{
			UpdateCurrentItem();
			RebuildFlyoutItems();
		}

		void OnFlyoutItemsChanged(object sender, EventArgs e) => RebuildFlyoutItems();

		void UpdateCurrentItem()
		{
			if (ShellElement == null)
				return;

			var shellItem = ShellElement.CurrentItem;

			if (!ReferenceEquals(shellItem, _currentShellItem))
			{
				if (_currentShellItem != null)
				{
					_currentShellItem.PropertyChanged -= OnShellItemPropertyChanged;
					((IShellItemController)_currentShellItem).ItemsCollectionChanged -= OnShellItemItemsChanged;
				}

				_currentShellItem = shellItem;

				if (_currentShellItem != null)
				{
					_currentShellItem.PropertyChanged += OnShellItemPropertyChanged;
					((IShellItemController)_currentShellItem).ItemsCollectionChanged += OnShellItemItemsChanged;
				}
			}

			UpdateCurrentSection();
			UpdateTabs();
		}

		void OnShellItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != ShellItem.CurrentItemProperty.PropertyName)
				return;

			UpdateCurrentSection();
			UpdateTabs();
		}

		void OnShellItemItemsChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateTabs();

		void OnShellSectionItemsChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateTabs();

		void UpdateCurrentSection()
		{
			var section = _currentShellItem?.CurrentItem;

			if (ReferenceEquals(section, _currentSection))
				return;

			if (_currentSection != null)
			{
				var previous = (IShellSectionController)_currentSection;

				previous.RemoveDisplayedPageObserver(this);
				previous.NavigationRequested -= OnNavigationRequested;
				previous.ItemsCollectionChanged -= OnShellSectionItemsChanged;
				_currentSection.PropertyChanged -= OnShellSectionPropertyChanged;
			}

			_currentSection = section;

			if (_currentSection == null)
			{
				ShowPage(null);
				return;
			}

			var controller = (IShellSectionController)_currentSection;

			// A push does not move DisplayedPage - Core only raises NavigationRequested for it - so
			// the nav stack and the content switch are two separate subscriptions.
			controller.NavigationRequested += OnNavigationRequested;
			controller.ItemsCollectionChanged += OnShellSectionItemsChanged;
			_currentSection.PropertyChanged += OnShellSectionPropertyChanged;

			// AddDisplayedPageObserver invokes the callback immediately with the section's current
			// DisplayedPage, so this both primes and subscribes.
			controller.AddDisplayedPageObserver(this, OnDisplayedPageChanged);

			// Switching to a section that already has a deep stack must show its top page, which
			// DisplayedPage only reports once UpdateDisplayedPage has run for it.
			var presented = controller.PresentedPage;

			if (presented != null && !ReferenceEquals(presented, _currentPage))
				ShowPage(presented);
		}

		void OnShellSectionPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == ShellSection.CurrentItemProperty.PropertyName)
				UpdateTabs();
		}

		void OnDisplayedPageChanged(FormsPage page)
		{
			if (_disposed)
				return;

			// A ShellContent built from a ContentTemplate has no page until someone asks for one,
			// and DisplayedPage is exactly that page - so with a template it arrives null. Creating
			// the content sets ContentCache, which re-runs UpdateDisplayedPage and re-enters here
			// with the real page.
			if (page == null && _currentSection?.CurrentItem is IShellContentController content && content.Page == null)
			{
				content.GetOrCreateContent();
				return;
			}

			ShowPage(page);
		}

		void ShowPage(FormsPage page)
		{
			if (Widget == null || ReferenceEquals(page, _currentPage))
				return;

			if (_currentPage != null)
				_currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;

			_currentPage = page;

			// Shell.NavBarIsVisible and Shell.TabBarIsVisible are plain attached properties with no
			// propertyChanged callback and no Core-side notification, so the only way to react to
			// them is to watch the page they are set on - which is what the Android renderer does.
			if (_currentPage != null)
				_currentPage.PropertyChanged += OnCurrentPagePropertyChanged;

			AttachToolbarPage(page);

			if (page == null)
			{
				UpdateTitle();
				UpdateToolbarItems();
				UpdateTitleView();
				UpdateSearchHandler();
				return;
			}

			if (!_pageRenderers.TryGetValue(page, out var renderer))
			{
				renderer = Platform.GetRenderer(page);

				if (renderer == null)
				{
					renderer = Platform.CreateRenderer(page);
					Platform.SetRenderer(page, renderer);
				}

				_pageRenderers[page] = renderer;

				// A monotonic id, not the dictionary count: pushed pages are pruned when they are
				// popped, so the count is reused and Gtk.Stack names would collide.
				Widget.ContentStack.AddNamed(renderer.Container, "page" + _pageId++);
			}

			renderer.Container.ShowAll();
			Widget.ContentStack.VisibleChild = renderer.Container;

			UpdateTitle();
			UpdateNavBarVisibility();
			UpdateFlyoutSelection();
			UpdateTabs();
			UpdateBackButton();
			UpdateToolbarItems();
			UpdateTitleView();
			UpdateSearchHandler();
			QueueFlyoutLayout();
		}

		void OnCurrentPagePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Shell.TabBarIsVisibleProperty.PropertyName)
				UpdateTabs();
			else if (e.PropertyName == Shell.NavBarIsVisibleProperty.PropertyName)
				UpdateNavBarVisibility();
			else if (e.PropertyName == FormsPage.TitleProperty.PropertyName)
				UpdateTitle();
			else if (e.PropertyName == Shell.TitleViewProperty.PropertyName)
				UpdateTitleView();
			else if (e.PropertyName == Shell.SearchHandlerProperty.PropertyName)
				UpdateSearchHandler();
		}

		void UpdateTitle()
		{
			if (Widget == null)
				return;

			// Shell's own Title is the fallback; the presented page's title is what a user reads as
			// "where am I", which is what every other Shell backend shows.
			var title = _currentPage?.Title;

			if (string.IsNullOrEmpty(title))
				title = _currentSection?.CurrentItem?.Title;

			if (string.IsNullOrEmpty(title))
				title = ShellElement?.Title;

			Widget.Title = title ?? string.Empty;
		}

		void UpdateNavBarVisibility()
		{
			if (Widget == null)
				return;

			Widget.NavBarVisible = _currentPage == null || Shell.GetNavBarIsVisible(_currentPage);
		}

		void DetachShellItem()
		{
			if (_currentShellItem != null)
			{
				_currentShellItem.PropertyChanged -= OnShellItemPropertyChanged;
				((IShellItemController)_currentShellItem).ItemsCollectionChanged -= OnShellItemItemsChanged;
				_currentShellItem = null;
			}

			if (_currentSection != null)
			{
				var controller = (IShellSectionController)_currentSection;

				controller.RemoveDisplayedPageObserver(this);
				controller.NavigationRequested -= OnNavigationRequested;
				controller.ItemsCollectionChanged -= OnShellSectionItemsChanged;
				_currentSection.PropertyChanged -= OnShellSectionPropertyChanged;
				_currentSection = null;
			}
		}

		// ---- tab bars (stage two) ----------------------------------------------------------

		/// <summary>
		/// Rebuilds and re-selects both tab strips: <see cref="ShellSection"/>s of the current
		/// <see cref="ShellItem"/> above, <see cref="ShellContent"/>s of the current section below.
		/// </summary>
		/// <remarks>
		/// Both strips disappear once the section's navigation stack is deeper than its root, which
		/// is what every other Shell backend does: a pushed page owns the whole content area and is
		/// left by the back arrow, not by picking a sibling tab.
		/// </remarks>
		void UpdateTabs()
		{
			if (Widget == null || _disposed)
				return;

			var pushed = (_currentSection?.Stack?.Count ?? 0) > 1;

			var itemController = _currentShellItem as IShellItemController;
			var sections = itemController?.GetItems();

			Widget.SectionTabs.SetTabs(sections?.Select(TabTitle).ToList());
			Widget.SectionTabs.SelectedIndex = sections == null || _currentSection == null
				? -1
				: sections.IndexOf(_currentSection);

			// ShowTabs is Core's own answer, folding in Shell.TabBarIsVisible for the shown page.
			Widget.SectionTabsVisible = !pushed && sections != null && sections.Count > 1 && itemController.ShowTabs;

			var sectionController = _currentSection as IShellSectionController;
			var contents = sectionController?.GetItems();
			var currentContent = _currentSection?.CurrentItem;

			Widget.ContentTabs.SetTabs(contents?.Select(TabTitle).ToList());
			Widget.ContentTabs.SelectedIndex = contents == null || currentContent == null
				? -1
				: contents.IndexOf(currentContent);

			Widget.ContentTabsVisible = !pushed && contents != null && contents.Count > 1;

			QueueFlyoutLayout();
		}

		static string TabTitle(BaseShellItem item)
		{
			if (item == null)
				return string.Empty;

			return string.IsNullOrEmpty(item.Title) ? (item.Route ?? string.Empty) : item.Title;
		}

		void OnSectionTabSelected(object sender, Controls.ShellTabSelectedEventArgs e)
		{
			if (_disposed || _currentShellItem == null)
				return;

			var sections = ((IShellItemController)_currentShellItem).GetItems();

			if (e.Index < 0 || e.Index >= sections.Count)
				return;

			// SetValueFromRenderer, not the CLR setter: this is a user gesture reported back to
			// Core, and it is what makes Shell update CurrentState/appearance for the new section.
			((IElementController)_currentShellItem).SetValueFromRenderer(ShellItem.CurrentItemProperty, sections[e.Index]);
		}

		void OnContentTabSelected(object sender, Controls.ShellTabSelectedEventArgs e)
		{
			if (_disposed || _currentSection == null)
				return;

			var contents = ((IShellSectionController)_currentSection).GetItems();

			if (e.Index < 0 || e.Index >= contents.Count)
				return;

			((IElementController)_currentSection).SetValueFromRenderer(ShellSection.CurrentItemProperty, contents[e.Index]);
		}

		// ---- navigation stack (stage three) ------------------------------------------------

		/// <summary>
		/// The nav-stack half of "what is on screen". <c>GoToAsync</c>, <c>PushAsync</c>,
		/// <c>PopAsync</c> and <c>PopToRootAsync</c> all funnel through here.
		/// </summary>
		/// <remarks>
		/// Core has already mutated <c>_navStack</c> by the time this runs for Push, Pop and
		/// Remove - but <b>not</b> for PopToRoot, where the stack is only reset after the event
		/// returns. That is why PopToRoot reads the root <see cref="ShellContent"/>'s page directly
		/// instead of asking for <c>PresentedPage</c>.
		///
		/// <c>e.Task</c> is deliberately left null: showing a page here is synchronous, and Core
		/// skips the await when it is null. Handing back a task that never completes would hang
		/// every <c>GoToAsync</c>.
		/// </remarks>
		void OnNavigationRequested(object sender, Internals.NavigationRequestedEventArgs e)
		{
			if (_disposed || _currentSection == null)
				return;

			var controller = (IShellSectionController)_currentSection;

			switch (e.RequestType)
			{
				case Internals.NavigationRequestType.Push:
					ShowPage(e.Page);
					break;

				case Internals.NavigationRequestType.Pop:
				case Internals.NavigationRequestType.Remove:
					ShowPage(controller.PresentedPage);
					break;

				case Internals.NavigationRequestType.PopToRoot:
					ShowPage((_currentSection.CurrentItem as IShellContentController)?.Page);
					break;

				default:
					// Insert changes the stack below what is visible, so nothing to show; the back
					// button still has to be re-evaluated because the depth changed.
					break;
			}

			UpdateTabs();
			UpdateBackButton();
			QueueNavStateRefresh();
		}

		void UpdateBackButton()
		{
			if (Widget == null || _disposed)
				return;

			var behavior = _currentPage == null ? null : Shell.GetBackButtonBehavior(_currentPage);
			var canGoBack = (_currentSection?.Stack?.Count ?? 0) > 1;

			// A BackButtonBehavior with a Command is an explicit "put a back affordance here", even
			// at the root of the stack; IsEnabled = false is an explicit "take it away".
			if (behavior?.Command != null)
				canGoBack = true;

			if (behavior != null && !behavior.IsEnabled)
				canGoBack = false;

			Widget.BackText = behavior?.TextOverride;
			Widget.BackVisible = canGoBack;
		}

		void OnBackRequested(object sender, EventArgs e)
		{
			if (_disposed || ShellElement == null)
				return;

			var behavior = _currentPage == null ? null : Shell.GetBackButtonBehavior(_currentPage);
			var command = behavior?.Command;

			if (command != null)
			{
				var parameter = behavior.CommandParameter;

				if (command.CanExecute(parameter))
					command.Execute(parameter);

				return;
			}

			// Core's own back handling: it gives the visible page first refusal, then pops.
			ShellElement.SendBackButtonPressed();
		}

		bool _navRefreshQueued;

		/// <summary>
		/// Re-reads the navigation stack once it has settled: tab visibility, the back arrow, and
		/// the renderers of pages that have left the stack.
		/// </summary>
		/// <remarks>
		/// Deferred to idle on purpose, and not merely as tidying - it is load-bearing twice over:
		///
		/// * <c>PopToRoot</c> raises <c>NavigationRequested</c> <b>before</b> it resets
		///   <c>_navStack</c>, so a handler that reads <c>Stack.Count</c> inline still sees the old
		///   depth and leaves the back arrow on a page that has nothing behind it. (Measured: the
		///   smoke test caught exactly this.)
		/// * Core detaches a popped page after the event returns, so <c>Page.Parent</c> - the only
		///   reliable "this page is gone" signal available here - is still set while it runs.
		/// </remarks>
		void QueueNavStateRefresh()
		{
			if (_navRefreshQueued || _disposed)
				return;

			_navRefreshQueued = true;

			GLib.Idle.Add(() =>
			{
				_navRefreshQueued = false;

				if (_disposed)
					return false;

				UpdateTabs();
				UpdateBackButton();
				PrunePageRenderers();

				return false;
			});
		}

		void PrunePageRenderers()
		{
			foreach (var pair in _pageRenderers.ToList())
			{
				var page = pair.Key;

				if (ReferenceEquals(page, _currentPage) || page.Parent != null)
					continue;

				Detach(pair.Value.Container);
				pair.Value.Dispose();
				Platform.SetRenderer(page, null);
				_pageRenderers.Remove(page);
			}
		}

		// ---- flyout header / footer ------------------------------------------------------

		void UpdateFlyoutHeader()
		{
			DestroyFlyoutHost(ref _headerHost);

			var view = ShellController?.FlyoutHeader;

			if (view != null)
			{
				_headerHost = CreateHost(null, view);
				Widget.FlyoutHeaderHost.Add(_headerHost.Host);
				Widget.FlyoutHeaderHost.ShowAll();
			}

			QueueFlyoutLayout();
		}

		void UpdateFlyoutFooter()
		{
			DestroyFlyoutHost(ref _footerHost);

			var view = ShellController?.FlyoutFooter;

			if (view != null)
			{
				_footerHost = CreateHost(null, view);
				Widget.FlyoutFooterHost.Add(_footerHost.Host);
				Widget.FlyoutFooterHost.ShowAll();
			}

			QueueFlyoutLayout();
		}

		// ---- flyout items ----------------------------------------------------------------

		void RebuildFlyoutItems()
		{
			if (Widget == null || ShellController == null)
				return;

			ClearFlyoutItems();

			var grouping = ShellController.GenerateFlyoutGrouping();

			if (grouping == null)
				return;

			for (var g = 0; g < grouping.Count; g++)
			{
				if (g > 0)
				{
					var separator = new Gtk.Separator(Gtk.Orientation.Horizontal);
					Widget.FlyoutItemsBox.PackStart(separator, false, false, 4);
					_separators.Add(separator);
				}

				foreach (var element in grouping[g])
				{
					var view = CreateFlyoutItemView(element);

					if (view == null)
						continue;

					var entry = CreateHost(element, view);

					Widget.FlyoutItemsBox.PackStart(entry.Host, false, false, 0);
					_flyoutHosts.Add(entry);
				}
			}

			Widget.FlyoutItemsBox.ShowAll();
			UpdateFlyoutSelection();
			QueueFlyoutLayout();
		}

		/// <summary>
		/// Marks the flyout item that leads to what is currently on screen.
		/// </summary>
		/// <remarks>
		/// Two mechanisms, as in <c>CollectionViewRenderer</c>: the host <see cref="Gtk.EventBox"/>
		/// only paints once it owns a GdkWindow, and a cell that draws its own background would
		/// cover that anyway - so the cell is also put into the <c>Selected</c> visual state.
		/// </remarks>
		void UpdateFlyoutSelection()
		{
			if (_disposed || ShellElement == null)
				return;

			var shellItem = ShellElement.CurrentItem;
			var section = shellItem?.CurrentItem;
			var content = section?.CurrentItem;

			foreach (var entry in _flyoutHosts)
			{
				var selected = entry.Element != null &&
					(ReferenceEquals(entry.Element, content) ||
					 ReferenceEquals(entry.Element, section) ||
					 ReferenceEquals(entry.Element, shellItem));

				entry.Host.VisibleWindow = selected;

				if (selected)
					entry.Host.SetBackgroundColor(SelectionColor, Gtk.StateType.Normal);
				else
					entry.Host.ClearStyle();

				if (entry.View != null)
					VisualStateManager.GoToState(entry.View,
						selected ? VisualStateManager.CommonStates.Selected : VisualStateManager.CommonStates.Normal);
			}
		}

		static readonly Gdk.Color SelectionColor = new Gdk.Color(230, 230, 230);

		View CreateFlyoutItemView(Element element)
		{
			var bindable = element as BindableObject;

			if (bindable == null)
				return null;

			// The element itself is the binding context, exactly as on Android. Core's menu-item
			// template binds Text/Icon and its shell-item template binds Title/FlyoutIcon, and the
			// grouping only ever yields elements that expose whichever pair applies - MenuShellItem
			// (which is internal to Core, so it cannot be named here) surfaces Text and Icon of its
			// wrapped MenuItem precisely so this works without unwrapping it.
			var template = HasCustomFlyoutItemTemplate(bindable)
				? ShellController.GetFlyoutItemDataTemplate(bindable)
				: CreateDefaultFlyoutItemTemplate(bindable is IMenuItemController);

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(bindable, ShellElement);

			View view = template?.CreateContent() as View;

			if (view == null)
			{
				var text = bindable is MenuItem menuItem
					? menuItem.Text
					: (bindable as BaseShellItem)?.Title;

				view = new Label { Text = text ?? string.Empty, Margin = new Thickness(12, 10, 12, 10) };
			}

			// Parent first, then BindingContext: parenting makes the view inherit the Shell's
			// binding context and would otherwise overwrite the item we are about to set.
			view.Parent = Element;
			view.BindingContext = bindable;

			return view;
		}

		/// <summary>
		/// Whether the application (or the item itself) supplied a flyout item template, in which
		/// case Core's <c>GetFlyoutItemDataTemplate</c> is authoritative.
		/// </summary>
		/// <remarks>
		/// This mirrors the check Core makes internally. It matters because Core's *fallback* cell
		/// is written per-platform and has no GTK branch: on GTK it produces a grid whose only
		/// declared column is a Star holding the (usually empty) icon, so the implicit second
		/// column - also Star - pushes the label into the right half of the flyout. Android does the
		/// same substitution through <c>ShellFlyoutRecyclerAdapter.DefaultItemTemplate</c>.
		/// </remarks>
		bool HasCustomFlyoutItemTemplate(BindableObject bindable)
		{
			var property = bindable is IMenuItemController
				? Shell.MenuItemTemplateProperty
				: Shell.ItemTemplateProperty;

			if (bindable.IsSet(property))
				return true;

			if (bindable is MenuItem menuItem && menuItem.Parent != null && menuItem.Parent.IsSet(property))
				return true;

			return ShellElement != null && ShellElement.IsSet(property);
		}

		/// <summary>
		/// The GTK flyout cell: a fixed-width icon column and a vertically centred title.
		/// </summary>
		static DataTemplate CreateDefaultFlyoutItemTemplate(bool isMenuItem)
		{
			var textBinding = isMenuItem ? "Text" : "Title";
			var iconBinding = isMenuItem ? "Icon" : "FlyoutIcon";

			return new DataTemplate(() =>
			{
				// Rule: inside Xamarin.Forms.Platform.GTK the bare name Grid is Xamarin.Forms.Grid,
				// which is what is wanted here - spelled out so it is not read as a mistake.
				var grid = new Xamarin.Forms.Grid
				{
					HeightRequest = 44,
					ColumnSpacing = 0,
					RowSpacing = 0,
					ColumnDefinitions =
					{
						new ColumnDefinition { Width = new GridLength(44) },
						new ColumnDefinition { Width = GridLength.Star }
					}
				};

				var image = new Image
				{
					HeightRequest = 24,
					WidthRequest = 24,
					VerticalOptions = LayoutOptions.Center,
					HorizontalOptions = LayoutOptions.Center
				};

				image.SetBinding(Image.SourceProperty, new Binding(iconBinding));

				var label = new Label
				{
					VerticalTextAlignment = TextAlignment.Center,
					Margin = new Thickness(4, 0, 12, 0)
				};

				label.SetBinding(Label.TextProperty, new Binding(textBinding));

				grid.Children.Add(image, 0, 0);
				grid.Children.Add(label, 1, 0);

				// The renderer also paints selection on the host window, but a cell with its own
				// background would hide that, so the visual state is driven as well.
				var groups = new VisualStateGroupList();
				var common = new VisualStateGroup { Name = "CommonStates" };

				common.States.Add(new VisualState { Name = VisualStateManager.CommonStates.Normal });

				var selected = new VisualState { Name = VisualStateManager.CommonStates.Selected };
				selected.Setters.Add(new Setter
				{
					Property = VisualElement.BackgroundColorProperty,
					Value = new Color(0.9)
				});

				common.States.Add(selected);
				groups.Add(common);

				VisualStateManager.SetVisualStateGroups(grid, groups);

				return grid;
			});
		}

		FlyoutHost CreateHost(Element element, View view)
		{
			var renderer = Platform.GetRenderer(view);

			if (renderer == null)
			{
				renderer = Platform.CreateRenderer(view);
				Platform.SetRenderer(view, renderer);
			}

			var host = new Gtk.EventBox { VisibleWindow = false };
			host.Add(renderer.Container);

			var entry = new FlyoutHost
			{
				Element = element,
				View = view,
				Renderer = renderer,
				Host = host
			};

			if (element != null)
				host.ButtonPressEvent += OnFlyoutItemPressed;

			host.ShowAll();

			return entry;
		}

		void OnFlyoutItemPressed(object o, Gtk.ButtonPressEventArgs args)
		{
			if (_disposed || ShellController == null)
				return;

			var entry = _flyoutHosts.FirstOrDefault(h => ReferenceEquals(h.Host, o));

			if (entry?.Element == null)
				return;

			args.RetVal = true;

			// Core does the rest: it resolves the element to a shell item/section/content, closes
			// the flyout by setting FlyoutIsPresented, and navigates.
			ShellController.OnFlyoutItemSelected(entry.Element);
		}

		void ClearFlyoutItems()
		{
			foreach (var entry in _flyoutHosts.ToList())
			{
				var e = entry;
				DestroyFlyoutHost(ref e);
			}

			_flyoutHosts.Clear();

			foreach (var separator in _separators)
			{
				Detach(separator);
				separator.Destroy();
			}

			_separators.Clear();
		}

		void DestroyFlyoutHost(ref FlyoutHost entry)
		{
			if (entry == null)
				return;

			if (entry.Element != null)
				entry.Host.ButtonPressEvent -= OnFlyoutItemPressed;

			Detach(entry.Host);

			entry.Renderer?.Dispose();

			if (entry.View != null)
			{
				Platform.SetRenderer(entry.View, null);

				if (entry.OwnsParent)
					entry.View.Parent = null;
			}

			entry.Host.Destroy();
			entry = null;
		}

		static void Detach(Gtk.Widget widget)
		{
			if (widget?.Parent is Gtk.Container container)
				container.Remove(widget);
		}

		// ---- effective attached values -----------------------------------------------------

		/// <summary>
		/// Walks the displayed page up to the <see cref="Shell"/> looking for the first element that
		/// has <paramref name="property"/> set - the pivot walk <c>Shell.GetEffectiveValue</c> does
		/// internally, reimplemented because that method is <c>internal</c> to Core.
		/// </summary>
		/// <remarks>
		/// Android reads these attached properties off the displayed <c>Page</c> only. Walking up
		/// additionally honours a <c>SearchHandler</c>/<c>TitleView</c> declared on the
		/// <c>ShellContent</c>, the <c>ShellSection</c> or the <c>Shell</c> itself, which is what
		/// the XAML in the Shell templates actually does, and it still lets the page win.
		/// </remarks>
		T GetEffective<T>(BindableProperty property) where T : class
		{
			Element element = _currentPage;

			while (element != null)
			{
				if (element.IsSet(property))
					return element.GetValue(property) as T;

				if (ReferenceEquals(element, Element))
					break;

				element = element.Parent;
			}

			return null;
		}

		// ---- toolbar items -------------------------------------------------------------------

		void AttachToolbarPage(FormsPage page)
		{
			if (ReferenceEquals(page, _toolbarPage))
				return;

			DetachToolbarPage();

			_toolbarPage = page;

			if (_toolbarPage?.ToolbarItems is INotifyCollectionChanged observable)
				observable.CollectionChanged += OnToolbarItemsChanged;
		}

		void DetachToolbarPage()
		{
			if (_toolbarPage?.ToolbarItems is INotifyCollectionChanged observable)
				observable.CollectionChanged -= OnToolbarItemsChanged;

			foreach (var item in _toolbarItems)
				item.PropertyChanged -= OnToolbarItemPropertyChanged;

			_toolbarItems.Clear();
			_toolbarPage = null;
		}

		void OnToolbarItemsChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateToolbarItems();

		void OnToolbarItemPropertyChanged(object sender, PropertyChangedEventArgs e) => UpdateToolbarItems();

		/// <summary>
		/// Flattens the displayed page's <see cref="ToolbarItem"/>s onto the nav bar: primary first,
		/// then secondary behind the overflow, each group ordered by <c>Priority</c>.
		/// </summary>
		/// <remarks>
		/// <c>ToolbarItemOrder.Default</c> counts as primary, which is what the Android and iOS
		/// trackers do; only <c>Secondary</c> goes to the overflow. The order of this flattened list
		/// is the index the widget reports back, so it is built once and kept.
		/// </remarks>
		void UpdateToolbarItems()
		{
			if (Widget == null || _disposed)
				return;

			foreach (var item in _toolbarItems)
				item.PropertyChanged -= OnToolbarItemPropertyChanged;

			_toolbarItems.Clear();

			var source = _currentPage?.ToolbarItems;

			if (source != null)
			{
				_toolbarItems.AddRange(source
					.Where(i => i != null && i.Order != ToolbarItemOrder.Secondary)
					.OrderBy(i => i.Priority));

				_toolbarItems.AddRange(source
					.Where(i => i != null && i.Order == ToolbarItemOrder.Secondary)
					.OrderBy(i => i.Priority));
			}

			var infos = new List<Controls.ShellToolbarItemInfo>(_toolbarItems.Count);

			foreach (var item in _toolbarItems)
			{
				item.PropertyChanged += OnToolbarItemPropertyChanged;

				infos.Add(new Controls.ShellToolbarItemInfo
				{
					Text = item.Text,
					Icon = item.IconImageSource == null ? null : item.IconImageSource.ToPixbuf(new Size(24, 24)),
					IsEnabled = item.IsEnabled,
					IsSecondary = item.Order == ToolbarItemOrder.Secondary
				});
			}

			Widget.SetToolbarItems(infos);
		}

		void OnToolbarItemActivated(object sender, Controls.ShellToolbarItemActivatedEventArgs e)
		{
			if (_disposed || e.Index < 0 || e.Index >= _toolbarItems.Count)
				return;

			// Activate(), not Command.Execute(): it is what raises Clicked as well, and it is the
			// contract every other backend drives a ToolbarItem through.
			((IMenuItemController)_toolbarItems[e.Index]).Activate();
		}

		// ---- TitleView -----------------------------------------------------------------------

		/// <summary>
		/// Puts <c>Shell.TitleView</c> in the nav bar in place of the title label.
		/// </summary>
		/// <remarks>
		/// The host is packed expand/fill, so GTK hands it whatever the back/hamburger, the search
		/// box and the toolbar leave over; that allocation - not a guess - is the width the Forms
		/// view is measured and laid out against, in <see cref="LayoutTitleView"/>, on idle.
		/// </remarks>
		void UpdateTitleView()
		{
			if (Widget == null || _disposed)
				return;

			var view = GetEffective<View>(Shell.TitleViewProperty);

			if (_titleViewHost != null && ReferenceEquals(_titleViewHost.View, view))
				return;

			DestroyFlyoutHost(ref _titleViewHost);

			if (view != null)
			{
				// Core's Shell.OnTitleViewChanged has usually already parented the view to whatever
				// declared it, and that parent is where its binding context comes from - so adopt it
				// only when there is none, and record that so it is not un-parented on the way out.
				var adopted = view.Parent == null;

				if (adopted)
					view.Parent = Element;

				_titleViewHost = CreateHost(null, view);
				_titleViewHost.OwnsParent = adopted;

				// CreateHost already ShowAll()s the inner host, and that is the only thing that can
				// show it: ShowAll() on Widget.TitleViewHost would be a NO-OP, because it carries
				// NoShowAll so a stray parent ShowAll cannot resurrect a title view that has been
				// cleared. The wrapper itself is shown by TitleViewVisible, below.
				Widget.TitleViewHost.Add(_titleViewHost.Host);
			}

			Widget.TitleViewVisible = view != null;

			QueueFlyoutLayout();
		}

		// ---- SearchHandler -------------------------------------------------------------------

		void UpdateSearchHandler()
		{
			if (Widget == null || _disposed)
				return;

			var handler = GetEffective<SearchHandler>(Shell.SearchHandlerProperty);

			if (!ReferenceEquals(handler, _searchHandler))
			{
				DetachSearchHandler();

				_searchHandler = handler;

				if (_searchHandler != null)
				{
					_searchHandler.PropertyChanged += OnSearchHandlerPropertyChanged;
					((ISearchHandlerController)_searchHandler).ListProxyChanged += OnSearchListProxyChanged;
				}

				AttachSearchItemsSource();
			}

			ApplySearchHandler();
		}

		void DetachSearchHandler()
		{
			if (_searchItemsSource != null)
			{
				_searchItemsSource.CollectionChanged -= OnSearchItemsChanged;
				_searchItemsSource = null;
			}

			if (_searchHandler == null)
				return;

			_searchHandler.PropertyChanged -= OnSearchHandlerPropertyChanged;
			((ISearchHandlerController)_searchHandler).ListProxyChanged -= OnSearchListProxyChanged;
			_searchHandler = null;
		}

		void AttachSearchItemsSource()
		{
			if (_searchItemsSource != null)
			{
				_searchItemsSource.CollectionChanged -= OnSearchItemsChanged;
				_searchItemsSource = null;
			}

			// The proxy Core hands out is the live view of ItemsSource; an app that re-filters in
			// OnQueryChanged mutates it rather than replacing it, so the collection event is the
			// only signal that the suggestions changed.
			_searchItemsSource = (_searchHandler as ISearchHandlerController)?.ListProxy as INotifyCollectionChanged;

			if (_searchItemsSource != null)
				_searchItemsSource.CollectionChanged += OnSearchItemsChanged;
		}

		void OnSearchListProxyChanged(object sender, ListProxyChangedEventArgs e)
		{
			AttachSearchItemsSource();
			RebuildSuggestions();
		}

		void OnSearchItemsChanged(object sender, NotifyCollectionChangedEventArgs e) => RebuildSuggestions();

		void OnSearchHandlerPropertyChanged(object sender, PropertyChangedEventArgs e) => ApplySearchHandler();

		void ApplySearchHandler()
		{
			if (Widget == null || _disposed)
				return;

			var entry = Widget.SearchEntry;

			if (_searchHandler == null)
			{
				Widget.SearchMode = Controls.ShellSearchBoxMode.Hidden;
				Widget.ClearPlaceholderVisible = false;
				RebuildSuggestions();

				return;
			}

			switch (_searchHandler.SearchBoxVisibility)
			{
				case SearchBoxVisibility.Hidden:
					Widget.SearchMode = Controls.ShellSearchBoxMode.Hidden;
					break;
				case SearchBoxVisibility.Collapsible:
					Widget.SearchMode = Controls.ShellSearchBoxMode.Collapsible;
					break;
				default:
					Widget.SearchMode = Controls.ShellSearchBoxMode.Expanded;
					break;
			}

			// Guarded: writing the text raises Gtk.Entry.Changed, which is the same handler that
			// pushes the text back into Query.
			_updatingQuery = true;

			try
			{
				var query = _searchHandler.UpdateFormsText(_searchHandler.Query, _searchHandler.TextTransform) ?? string.Empty;

				if (!string.Equals(entry.SearchText, query, StringComparison.Ordinal))
					entry.SearchText = query;
			}
			finally
			{
				_updatingQuery = false;
			}

			entry.PlaceholderText = _searchHandler.Placeholder ?? string.Empty;
			entry.Sensitive = _searchHandler.IsSearchEnabled;

			if (!_searchHandler.TextColor.IsDefaultOrTransparent())
				entry.SetTextColor(_searchHandler.TextColor.ToGtkColor());

			if (!_searchHandler.PlaceholderColor.IsDefaultOrTransparent())
				entry.SetPlaceholderTextColor(_searchHandler.PlaceholderColor.ToGtkColor());

			if (!_searchHandler.BackgroundColor.IsDefaultOrTransparent())
				entry.SetBackgroundColor(_searchHandler.BackgroundColor.ToGtkColor());

			if (!_searchHandler.CancelButtonColor.IsDefaultOrTransparent())
				entry.SetCancelButtonColor(_searchHandler.CancelButtonColor.ToGtkColor());

			entry.SetFont(Helpers.FontDescriptionHelper.CreateFontDescription(
				_searchHandler.FontSize, _searchHandler.FontFamily, _searchHandler.FontAttributes));

			Widget.ClearPlaceholderVisible = _searchHandler.ClearPlaceholderEnabled;

			RebuildSuggestions();
		}

		void OnSearchTextChanged(object sender, EventArgs e)
		{
			if (_disposed || _updatingQuery || _searchHandler == null)
				return;

			_updatingQuery = true;

			try
			{
				_searchHandler.Query = Widget.SearchEntry.SearchText;
			}
			finally
			{
				_updatingQuery = false;
			}

			RebuildSuggestions();
		}

		void OnSearchConfirmed(object sender, EventArgs e)
		{
			if (_disposed || _searchHandler == null)
				return;

			((ISearchHandlerController)_searchHandler).QueryConfirmed();
		}

		void OnClearPlaceholderClicked(object sender, EventArgs e)
		{
			if (_disposed || _searchHandler == null)
				return;

			((ISearchHandlerController)_searchHandler).ClearPlaceholderClicked();
		}

		void OnSearchExpandedChanged(object sender, EventArgs e) => RebuildSuggestions();

		void OnSuggestionSelected(object sender, Controls.ShellSuggestionSelectedEventArgs e)
		{
			if (_disposed || _searchHandler == null)
				return;

			var items = ((ISearchHandlerController)_searchHandler).ListProxy;

			if (items == null || e.Index < 0 || e.Index >= items.Count)
				return;

			((ISearchHandlerController)_searchHandler).ItemSelected(items[e.Index]);
		}

		/// <summary>
		/// Rebuilds the suggestion rows under the nav bar from the handler's <c>ListProxy</c>.
		/// </summary>
		/// <remarks>
		/// The list is only offered when the handler asks for it (<c>ShowsResults</c>) and the box is
		/// actually open, which is what stops a <c>Collapsible</c> handler from dropping a result
		/// list over the page while its box is still a magnifier.
		/// </remarks>
		void RebuildSuggestions()
		{
			if (Widget == null || _disposed)
				return;

			ClearSuggestions();

			var controller = _searchHandler as ISearchHandlerController;
			var items = controller?.ListProxy;

			var show = _searchHandler != null && _searchHandler.ShowsResults && Widget.SearchExpanded
				&& items != null && items.Count > 0;

			if (!show)
			{
				Widget.SetSuggestionRows(null);
				Widget.SuggestionsVisible = false;

				return;
			}

			var rows = new List<Gtk.Widget>(items.Count);

			foreach (var item in items)
			{
				var view = CreateSuggestionView(item);

				if (view != null)
				{
					var entry = CreateHost(null, view);

					_suggestionHosts.Add(entry);
					rows.Add(entry.Host);

					continue;
				}

				var label = new Gtk.Label(SuggestionText(item)) { Xalign = 0f, Margin = 8 };
				var host = new Gtk.EventBox { VisibleWindow = false };

				host.Add(label);
				rows.Add(host);
			}

			Widget.SetSuggestionRows(rows);
			Widget.SuggestionsVisible = true;

			QueueFlyoutLayout();
		}

		View CreateSuggestionView(object item)
		{
			var template = _searchHandler?.ItemTemplate;

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(item, ShellElement);

			var view = template?.CreateContent() as View;

			if (view == null)
				return null;

			view.Parent = Element;
			view.BindingContext = item;

			return view;
		}

		string SuggestionText(object item)
		{
			if (item == null)
				return string.Empty;

			var member = _searchHandler?.DisplayMemberName;

			if (!string.IsNullOrEmpty(member))
			{
				var property = item.GetType().GetProperty(member);

				if (property != null)
					return property.GetValue(item)?.ToString() ?? string.Empty;
			}

			return item.ToString() ?? string.Empty;
		}

		void ClearSuggestions()
		{
			foreach (var entry in _suggestionHosts.ToList())
			{
				var e = entry;
				DestroyFlyoutHost(ref e);
			}

			_suggestionHosts.Clear();
		}

		// ---- flyout layout ---------------------------------------------------------------

		void OnWidgetLayoutRefreshed(object sender, EventArgs e) => LayoutFlyout();

		bool _flyoutLayoutQueued;

		/// <summary>
		/// Schedules the flyout's Forms layout pass. Always deferred: it measures, and measuring
		/// inside a size-allocate is exactly what GTK3 throws away.
		/// </summary>
		void QueueFlyoutLayout()
		{
			if (_flyoutLayoutQueued || _disposed)
				return;

			_flyoutLayoutQueued = true;

			GLib.Idle.Add(() =>
			{
				_flyoutLayoutQueued = false;

				if (!_disposed)
					LayoutFlyout();

				return false;
			});
		}

		/// <summary>
		/// Measures and lays out the flyout header, items and footer.
		/// </summary>
		/// <remarks>
		/// These views are not children of a Forms <c>Layout</c>, so no Forms layout pass ever
		/// reaches them: without this they keep the default 0-size and the flyout renders empty.
		/// </remarks>
		void LayoutFlyout()
		{
			if (Widget == null || _disposed)
				return;

			LayoutTitleView();
			LayoutSuggestions();

			var width = Widget.FlyoutWidth;

			if (width <= 1)
				return;

			LayoutFlyoutView(_headerHost, width);

			foreach (var entry in _flyoutHosts)
				LayoutFlyoutView(entry, width);

			LayoutFlyoutView(_footerHost, width);
		}

		/// <summary>
		/// Lays the <c>TitleView</c> out to the width GTK gave its nav bar host.
		/// </summary>
		/// <remarks>
		/// Only the <b>height</b> is pushed back as a size request. Setting a width request equal to
		/// the allocation is the size-request ratchet of plan 8.2.2: the host would never be able to
		/// shrink again when a toolbar item or the search box appears beside it.
		/// </remarks>
		void LayoutTitleView()
		{
			if (_titleViewHost?.View == null)
				return;

			var width = Widget.TitleViewHost.Width;

			if (width <= 1)
				return;

			var height = Math.Max(1, Math.Min(Widget.TitleViewHost.Height, Controls.ShellWidget.NavBarHeight));

			_titleViewHost.View.Layout(new Rectangle(0, 0, width, height));
			_titleViewHost.Host.SetSizeRequest(-1, height);
		}

		void LayoutSuggestions()
		{
			if (!Widget.SuggestionsVisible)
				return;

			var width = Widget.SuggestionsWrapper.Width;

			if (width <= 1)
				width = Widget.ContentStack.Width;

			if (width <= 1)
				return;

			var total = 0;

			foreach (var entry in _suggestionHosts)
			{
				LayoutFlyoutView(entry, width);
				total += NaturalHeight(entry.Host);
			}

			foreach (var row in Widget.SuggestionsBox.Children)
			{
				if (_suggestionHosts.All(h => !ReferenceEquals(h.Host, row)))
					total += NaturalHeight(row);
			}

			Widget.SuggestionsWrapper.HeightRequest =
				Math.Max(1, Math.Min(total, Controls.ShellWidget.MaxSuggestionsHeight));
		}

		/// <summary>Natural height of a widget. <c>Widget.SizeRequest()</c> is GTK2 and deprecated.</summary>
		static int NaturalHeight(Gtk.Widget widget)
		{
			widget.GetPreferredHeight(out _, out var natural);

			return natural;
		}

		static void LayoutFlyoutView(FlyoutHost entry, int width)
		{
			if (entry?.View == null)
				return;

			var request = entry.View.Measure(width, double.PositiveInfinity, MeasureFlags.IncludeMargins);
			var height = (int)Math.Ceiling(Math.Max(1, request.Request.Height));

			entry.View.Layout(new Rectangle(0, 0, width, height));
			entry.Host.SetSizeRequest(width, height);
		}
	}
}
