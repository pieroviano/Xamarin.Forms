using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Gtk;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK.Animations;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;
using Xamarin.Forms.Platform.GTK.Helpers;
using Xamarin.Forms.PlatformConfiguration.GTKSpecific;
using Container = Xamarin.Forms.Platform.GTK.GtkFormsContainer;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class NavigationPageRenderer : AbstractPageRenderer<Fixed, NavigationPage>, IToolbarTracker
	{
		private const int NavigationAnimationDuration = 250;    // Ms

		private Stack<NavigationChildPage> _currentStack;

		INavigationPageController NavigationController => Element as INavigationPageController;

		private GtkToolbarTracker _toolbarTracker;
		private Page _currentPage;
		private Gdk.Rectangle _lastAllocation;

		public NavigationPageRenderer()
		{
			_currentStack = new Stack<NavigationChildPage>();
			_toolbarTracker = new GtkToolbarTracker();
		}

		public GtkToolbarTracker NativeToolbarTracker => _toolbarTracker;

		public Task<bool> PopToRootAsync(Page page, bool animated = true)
		{
			return OnPopToRoot(page, animated);
		}

		public Task<bool> PopViewAsync(Page page, bool animated = true)
		{
			return OnPop(page, animated);
		}

		public Task<bool> PushPageAsync(Page page, bool animated = true)
		{
			return OnPush(page, animated);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (NavigationController != null)
				{
					NavigationController.PushRequested -= OnPushRequested;
					NavigationController.PopRequested -= OnPopRequested;
					NavigationController.PopToRootRequested -= OnPopToRootRequested;
					NavigationController.RemovePageRequested -= OnRemovedPageRequested;
					NavigationController.InsertPageBeforeRequested -= OnInsertPageBeforeRequested;
				}

				_toolbarTracker = null;

				if (_currentPage != null)
				{
					_currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;
					_currentPage = null;
				}
				if (_currentStack != null)
				{
					_currentStack.ForEach(s => s.Dispose());
					_currentStack = null;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnShown()
		{
			if (_appeared)
				return;

			_appeared = true;

			PageController.SendAppearing();

			base.OnShown();
		}

		protected override void OnDestroyed()
		{
			if (!_appeared)
				return;

			_toolbarTracker.TryHide(Page);
			_appeared = false;

			PageController?.SendDisappearing();

			base.OnDestroyed();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_lastAllocation == allocation)
				return;

			_lastAllocation = allocation;

			// Deferred, never inline. SetSizeRequest queues a resize and GTK3 discards a resize
			// queued from inside size-allocate, so applied inline the request was raised but the
			// child page was never re-allocated: it kept the natural size a Gtk.Fixed gives an
			// unconstrained child (measured 194x302 for the ControlGallery's detail page inside a
			// 500x600 NavigationPage). AbstractPageRenderer.SetPageSize then wrote that natural
			// size straight back into Forms, so the page laid its content out at 194x230 and the
			// AbsoluteLayout's proportional children overlapped each other.
			// ContainerArea is also set from there, and its setter calls ForceLayout().
			if (_childSizeQueued)
				return;

			_childSizeQueued = true;

			GLib.Idle.Add(() =>
			{
				_childSizeQueued = false;
				ApplyChildPageSizes();

				return false;
			});
		}

		bool _childSizeQueued;

		/// <summary>
		/// Sizes the navigation host and every page in the stack to this renderer's allocation, and
		/// tells Forms how much of that is actually available to a page.
		/// </summary>
		/// <remarks>
		/// The native toolbar is drawn INSIDE the current page's <see cref="Controls.Page"/> header,
		/// so a page's content gets the allocation minus <c>GtkToolbarConstants.ToolbarHeight</c> -
		/// but Forms' own <see cref="Xamarin.Forms.NavigationPage"/> knows nothing about that and
		/// lays its child pages out at the NavigationPage's full height. The two then disagreed by
		/// exactly the toolbar height, and because the page's container is sized from the Forms
		/// bounds its natural height became toolbar + full height: the whole window grew by 72px
		/// once and stayed there (measured 800x600 -> 800x672, scratchpad/m3-timeline.log).
		///
		/// <c>ContainerArea</c> is Core's own mechanism for exactly this - it is what iOS uses to
		/// inset a page under a native navigation bar - so setting it makes the two sides agree
		/// instead of ratcheting against each other. It must not be set inline from size-allocate:
		/// its setter calls <c>ForceLayout()</c>.
		/// </remarks>
		void ApplyChildPageSizes()
		{
			if (_disposed || Widget == null || Element == null)
				return;

			// An un-allocated widget reports 1x1; stamping that on the children would pin them at
			// their natural size for good, because nothing re-asserts a request that never changes.
			if (_lastAllocation.Width <= 1 || _lastAllocation.Height <= 1)
				return;

			// Deliberately NOT a SetSizeRequest on the host Fixed or on the page containers, which is
			// what this used to do. A size request is a MINIMUM, and it propagates all the way up to
			// the toplevel's minimum size - so requesting the child page at the navigation page's
			// full height made the window's minimum (toolbar + that height) and the window grew by
			// exactly the toolbar height and could never shrink back (measured 800x600 -> 800x672).
			// The page container does not need a request: its own natural size is toolbar + content,
			// and the content is sized from the Forms bounds that ContainerArea below establishes.

			// Same condition AbstractPageRenderer.SetPageSize uses to decide whether to subtract the
			// toolbar, so the inset Forms lays out to and the size the page renderer reports back
			// cannot drift apart.
			var toolbarHeight = NavigationPage.GetHasNavigationBar(Page) ? GtkToolbarConstants.ToolbarHeight : 0;

			((IPageController)Element).ContainerArea = new Rectangle(
				0, 0, _lastAllocation.Width, Math.Max(0, _lastAllocation.Height - toolbarHeight));
		}

		protected override void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);

			if (e.OldElement != null)
			{
				NavigationController.PushRequested -= OnPushRequested;
				NavigationController.PopRequested -= OnPopRequested;
				NavigationController.PopToRootRequested -= OnPopToRootRequested;
				NavigationController.RemovePageRequested -= OnRemovedPageRequested;
				NavigationController.InsertPageBeforeRequested -= OnInsertPageBeforeRequested;
			}

			if (e.NewElement != null)
			{
				if (Widget == null)
				{
					Widget = new Fixed();
					var eventBox = new GtkFormsContainer();
					eventBox.Add(Widget);

					Control.Content = eventBox;
				}

				UpdateBackgroundImage();
				Init();
			}
		}

		protected override void UpdateBackgroundImage()
		{
			base.UpdateBackgroundImage();

			if (Widget?.Parent is EventBox parent)
			{
				if (Page.CurrentPage?.Parent is Page parentPage && parentPage.BackgroundImageSource != null)
				{
					parent.VisibleWindow = parentPage.BackgroundImageSource.IsEmpty;
				}
				else
				{
					parent.VisibleWindow = true;
				}
			}
		}

		protected virtual async Task<bool> OnPopToRoot(Page page, bool animated)
		{
			var removed = await PopToRootPageAsync(page, animated);
			UpdateToolBar();
			return removed;
		}

		protected virtual async Task<bool> OnPop(Page page, bool animated)
		{
			var removed = await PopPageAsync(page, animated);
			UpdateToolBar();
			return removed;
		}

		protected virtual async Task<bool> OnPush(Page page, bool animated)
		{
			var shown = await AddPageAsync(page, animated);
			UpdateToolBar();
			return shown;
		}

		protected virtual void ConfigurePageRenderer()
		{
			Container.IsFocus = true;
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == NavigationPage.BarBackgroundColorProperty.PropertyName)
				UpdateBarBackgroundColor();
			else if (e.PropertyName == NavigationPage.BarTextColorProperty.PropertyName)
				UpdateBarTextColor();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName ==
				PlatformConfiguration.GTKSpecific.NavigationPage.BackButtonIconProperty.PropertyName)
				UpdateBackButtonIcon();
			else if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName)
				UpdateCurrentPage();
			else if (e.PropertyName == NavigationPage.HasNavigationBarProperty.PropertyName)
				UpdateToolBar();
		}

		private void Init()
		{
			ConfigurePageRenderer();

			if (Page.CurrentPage == null)
				throw new InvalidOperationException(
					"NavigationPage must have a root Page before being used. Either call PushAsync with a valid Page, or pass a Page to the constructor before usage.");

			_toolbarTracker.Navigation = Page;
			_currentPage = Page.CurrentPage;
			UpdateCurrentPage();

			NavigationController.PushRequested += OnPushRequested;
			NavigationController.PopRequested += OnPopRequested;
			NavigationController.PopToRootRequested += OnPopToRootRequested;
			NavigationController.RemovePageRequested += OnRemovedPageRequested;
			NavigationController.InsertPageBeforeRequested += OnInsertPageBeforeRequested;

			UpdateBarBackgroundColor();
			UpdateBarTextColor();

			NavigationController.Pages.ForEach(async p => await PushPageAsync(p, false));

			UpdateBackgroundColor();
			UpdateBackButtonIcon();
		}

		private void UpdateCurrentPage()
		{
			if (_currentPage != null)
			{
				_currentPage.PropertyChanged -= OnCurrentPagePropertyChanged;
			}

			_currentPage = Page.CurrentPage;

			if (_currentPage != null)
			{
				_currentPage.PropertyChanged += OnCurrentPagePropertyChanged;
				if (_toolbarTracker != null)
				{
					_toolbarTracker.ResetToolBar();
				}
			}

			UpdateTitle();
			UpdateIcon();
		}

		private void OnCurrentPagePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == Xamarin.Forms.Page.TitleProperty.PropertyName)
				UpdateTitle();
			else if (e.PropertyName == Xamarin.Forms.Page.IconImageSourceProperty.PropertyName)
				UpdateIcon();
		}

		private void UpdateTitle()
		{
			_toolbarTracker.UpdateTitle();
		}

		private void UpdateIcon()
		{
			_toolbarTracker.UpdateIcon();
		}

		private void OnPushRequested(object sender, NavigationRequestedEventArgs e)
		{
			e.Task = PushPageAsync(e.Page, e.Animated);
		}

		private void OnPopRequested(object sender, NavigationRequestedEventArgs e)
		{
			e.Task = PopViewAsync(e.Page, e.Animated);
		}

		private void OnPopToRootRequested(object sender, NavigationRequestedEventArgs e)
		{
			e.Task = PopToRootAsync(e.Page, e.Animated);
		}

		private async void OnRemovedPageRequested(object sender, NavigationRequestedEventArgs e)
		{
			await RemovePageAsync(e.Page, true, true);
			UpdateToolBar();
		}

		private void OnInsertPageBeforeRequested(object sender, NavigationRequestedEventArgs e)
		{
			InsertPageBefore(e.Page, e.BeforePage);
		}

		private async Task<bool> AddPageAsync(Page page, bool animated)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			Page oldPage = null;
			if (_currentStack.Count >= 1)
				oldPage = _currentStack.Peek().Page;

			(oldPage as IPageController)?.SendDisappearing();

			_currentStack.Push(new NavigationChildPage(page));

			if (Platform.GetRenderer(page) == null)
				Platform.SetRenderer(page, Platform.CreateRenderer(page));

			var pageRenderer = Platform.GetRenderer(page);
			Widget.Add(pageRenderer.Container);

			// Not `Allocation` directly: the root page is pushed from Init(), i.e. from inside
			// OnElementChanged, long before this renderer has ever been allocated, and an
			// un-allocated widget reports 1x1. ApplyChildPageSizes ignores that and the first real
			// allocation applies the true size to every child in the stack.
			ApplyChildPageSizes();

			pageRenderer.Container.ShowAll();

			if (animated)
			{
				var from = pageRenderer.Container.Parent.Allocation.Width;
				pageRenderer.Container.MoveTo(from, 0);

				await AnimatePageAsync(pageRenderer.Container, from, 0);
			}

			(page as IPageController)?.SendAppearing();

			if (oldPage != null && Platform.GetRenderer(oldPage) != null)
			{
				var oldPageRenderer = Platform.GetRenderer(oldPage);
				oldPageRenderer.Container.Sensitive = false;
			}

			return true;
		}

		private async Task RemovePageAsync(Page page, bool removeFromStack, bool animated)
		{
			var oldPage = _currentStack.Peek().Page;

			if (oldPage != null && Platform.GetRenderer(oldPage) != null)
			{
				var oldPageRenderer = Platform.GetRenderer(oldPage);
				oldPageRenderer.Container.Sensitive = true;
				oldPageRenderer.Container.ShowAll();
			}

			(page as IPageController)?.SendDisappearing();
			var target = Platform.GetRenderer(page);

			if (animated && target != null)
			{
				if (PlatformHelper.GetGTKPlatform() == GTKPlatform.Windows)
				{
					target.Container.MoveTo(0, 0);
					var to = target.Container.Parent.Allocation.Width;
					await AnimatePageAsync(target.Container, 0, to);
				}

				if (target != null)
				{
					Widget.RemoveFromContainer(target.Container);
				}

				FinishRemovePage(page, removeFromStack);
			}
			else
			{
				if (target != null)
				{
					Widget.RemoveFromContainer(target.Container);
				}

				FinishRemovePage(page, removeFromStack);
			}
		}

		private void InsertPageBefore(Page page, Page before)
		{
			if (before == null)
				throw new ArgumentNullException(nameof(before));
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			int index = PageController.InternalChildren.IndexOf(before);

			if (index == -1)
				throw new InvalidOperationException("This should never happen, please file a bug");

			var items = _currentStack.ToArray();
			_currentStack.Clear();

			int counter = 0;

			foreach (var item in items.Reverse())
			{
				if (counter == index)
				{
					_currentStack.Push(new NavigationChildPage(page));

					if (Platform.GetRenderer(page) == null)
						Platform.SetRenderer(page, Platform.CreateRenderer(page));
				}

				_currentStack.Push(item);

				counter++;
			}

			foreach (var child in Widget.Children)
			{
				child.Unparent();
			}

			items = _currentStack.ToArray();

			foreach (var item in items.Reverse())
			{
				var pageRenderer = Platform.GetRenderer(item.Page);
				Widget.Add(pageRenderer.Container);

				pageRenderer.Container.ShowAll();
			}

			ApplyChildPageSizes();
		}

		private async Task<bool> PopPageAsync(Page page, bool animated)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			var wrapper = _currentStack.Peek();
			if (page != wrapper.Page)
				throw new NotSupportedException("Popped page does not appear on top of current navigation stack, please file a bug.");

			_currentStack.Pop();
			(page as IPageController)?.SendDisappearing();

			var target = Platform.GetRenderer(page);
			var previousPage = _currentStack.Peek().Page;

			await RemovePageAsync(page, false, animated);

			return true;
		}

		private async Task<bool> PopToRootPageAsync(Page page, bool animated)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			(page as IPageController)?.SendDisappearing();

			for (int i = _currentStack.Count; i > 1; i--)
			{
				var lastPage = _currentStack.Pop();

				await RemovePageAsync(lastPage.Page, false, animated);
			}

			return true;
		}

		private void FinishRemovePage(Page page, bool removeFromStack)
		{
			GLib.Idle.Add(() =>
			{
				if (removeFromStack)
				{
					var newStack = new Stack<NavigationChildPage>();
					foreach (var stack in _currentStack)
					{
						if (stack.Page != page)
						{
							newStack.Push(stack);
						}
					}
					_currentStack = newStack;
				}

				var oldPage = _currentStack.Peek().Page;
				(oldPage as IPageController)?.SendAppearing();

				var target = Platform.GetRenderer(page);

				if (target != null)
				{
					target.Dispose();
				}

				return false;
			});
		}

		private void UpdateBarBackgroundColor()
		{
			UpdateToolBar();

			if (Element != null)
			{
				MessagingCenter.Send(Element, Forms.BarBackgroundColor, Page?.BarBackgroundColor);
			}
		}

		private void UpdateBarTextColor()
		{
			UpdateToolBar();

			if (Element != null)
			{
				MessagingCenter.Send(Element, Forms.BarTextColor, Page?.BarTextColor);
			}
		}

		private void UpdateBackButtonIcon()
		{
			var backButton = Page.OnThisPlatform().GetBackButtonIcon();

			_toolbarTracker.UpdateBackButton(backButton);
			UpdateToolBar();
		}

		private Task AnimatePageAsync(Container container, int from, int to)
		{
			return new FloatAnimation(from, to, TimeSpan.FromMilliseconds(NavigationAnimationDuration), true, (x) =>
			{
				GLib.Timeout.Add(0, () =>
				{
					container?.MoveTo(Convert.ToInt32(x), 0);

					return false;
				});
			}).Run();
		}

		private void UpdateToolBar()
		{
			GLib.Timeout.Add(0, () =>
			{
				if (_toolbarTracker != null)
				{
					_toolbarTracker.UpdateToolBar();
				}
				return false;
			});
		}
	}
}
