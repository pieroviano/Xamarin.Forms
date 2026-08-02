using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Gtk;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK.Controls;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public class FlyoutPageRenderer : AbstractPageRenderer<Controls.FlyoutPage, FlyoutPage>
	{
		Page _currentFlyout;
		Page _currentDetail;

		public FlyoutPageRenderer()
		{
			MessagingCenter.Subscribe(this, Forms.BarTextColor, (NavigationPage sender, Color color) =>
			{
				var barTextColor = color;

				if (barTextColor.IsDefaultOrTransparent())
				{
					Widget.UpdateBarTextColor(null);
				}
				else
				{
					Widget.UpdateBarTextColor(color.ToGtkColor());
				}
			});

			MessagingCenter.Subscribe(this, Forms.BarBackgroundColor, (NavigationPage sender, Color color) =>
			{
				var barBackgroundColor = color;

				if (barBackgroundColor.IsDefaultOrTransparent())
				{
					Widget.UpdateBarBackgroundColor(null);
				}
				else
				{
					Widget.UpdateBarBackgroundColor(color.ToGtkColor());
				}
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Widget != null)
				{
					Widget.IsPresentedChanged -= OnIsPresentedChanged;
				}

				MessagingCenter.Unsubscribe<NavigationPage, Color>(this, Forms.BarTextColor);
				MessagingCenter.Unsubscribe<NavigationPage, Color>(this, Forms.BarBackgroundColor);

				if (Page?.Flyout != null)
				{
					Page.Flyout.PropertyChanged -= HandleFlyoutPropertyChanged;
				}
			}

			base.Dispose(disposing);
		}

		protected override void OnElementChanged(VisualElementChangedEventArgs e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				if (Widget == null)
				{
					// There is nothing similar in Gtk. 
					// Custom control has been created that simulates the expected behavior.
					Widget = new Controls.FlyoutPage();
					var eventBox = new GtkFormsContainer();
					eventBox.Add(Widget);

					Control.Content = eventBox;

					Widget.IsPresentedChanged += OnIsPresentedChanged;

					UpdateFlyoutPage();
					UpdateFlyoutLayoutBehavior();
					UpdateIsPresented();
					UpdateBarTextColor();
					UpdateBarBackgroundColor();
				}
			}
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			// Must happen before base, which triggers the Forms layout pass:
			// FlyoutPage.LayoutChildren lays Flyout/Detail out from these two rectangles.
			UpdateFlyoutPageBounds(allocation);

			base.OnSizeAllocated(allocation);

			// Everything below mutates geometry, so all of it is deferred: a size request or a
			// resize queued from inside size-allocate is discarded by GTK3, and it leaves the
			// content container's parent permanently flagged resize-needed, which swallows every
			// later queue_resize raised anywhere beneath it (see AbstractPageRenderer.PageQueueResize).
			// SetSize inline here was one of the two sites wedging the flyout subtree.
			if (_behaviorUpdateQueued)
				return;

			_behaviorUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_behaviorUpdateQueued = false;

				Control?.Content?.SetSize(allocation.Width, allocation.Height);

				// ShouldShowSplitMode is a function of the allocation (and orientation), so
				// resizing the window can flip it. Re-evaluate here too.
				if (Widget != null && Page?.Flyout != null && Page?.Detail != null)
					UpdateFlyoutLayoutBehavior();

				return false;
			});
		}

		bool _behaviorUpdateQueued;

		/// <summary>
		/// Reports the flyout/detail geometry back to Forms.
		/// </summary>
		/// <remarks>
		/// <see cref="FlyoutPage.LayoutChildren"/> lays its two children out from
		/// <c>IFlyoutPageController.FlyoutBounds</c>/<c>DetailBounds</c>, which the platform
		/// owns - the Android and iOS renderers both set them. The GTK renderer never did,
		/// so Detail kept whatever size it happened to be measured at (194x200 for the
		/// ControlGallery) regardless of the window size, and its content painted over the
		/// rest of the page.
		///
		/// The rectangles deliberately mirror what Controls.FlyoutPage does to the native
		/// widgets in RefreshFlyoutLayoutBehavior, so the Forms-side and GTK-side geometry
		/// agree instead of fighting.
		/// </remarks>
		void UpdateFlyoutPageBounds(Gdk.Rectangle allocation)
		{
			if (!(Element is IFlyoutPageController controller))
				return;

			// Both setters throw until Flyout and Detail are assigned.
			if (Page?.Flyout == null || Page?.Detail == null)
				return;

			double width = allocation.Width;
			double height = allocation.Height;
			double flyoutWidth = Controls.FlyoutPage.DefaultFlyoutWidth;

			controller.FlyoutBounds = new Rectangle(0, 0, flyoutWidth, height);

			// Split keeps the flyout permanently on screen, so the detail is inset by it;
			// Popover/Default overlay the flyout, so the detail spans the full width.
			controller.DetailBounds = controller.ShouldShowSplitMode
				? new Rectangle(flyoutWidth, 0, Math.Max(0, width - flyoutWidth), height)
				: new Rectangle(0, 0, width, height);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName.Equals(nameof(FlyoutPage.Flyout)) || e.PropertyName.Equals(nameof(FlyoutPage.Detail)))
			{
				UpdateFlyoutPage();
				UpdateFlyoutLayoutBehavior();
				UpdateIsPresented();
			}
			else if (e.PropertyName == FlyoutPage.IsPresentedProperty.PropertyName)
				UpdateIsPresented();
			else if (e.PropertyName == FlyoutPage.FlyoutLayoutBehaviorProperty.PropertyName)
				UpdateFlyoutLayoutBehavior();
		}

		private async void HandleFlyoutPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != Xamarin.Forms.Page.IconImageSourceProperty.PropertyName)
				return;

			// async void: there is no Task for anyone to observe, so an exception escaping here is
			// rethrown on the synchronization context with no handler and takes the process down.
			// Loading the icon is file/network I/O, so a missing or malformed IconImageSource is
			// an ordinary authoring mistake - it should leave the icon blank, not kill the app.
			try
			{
				await UpdateHamburguerIconAsync();
			}
			catch (Exception ex)
			{
				Internals.Log.Warning("FlyoutPageRenderer", "Could not update the flyout icon: {0}", ex);
			}
		}

		private void UpdateFlyoutPage()
		{
			// The delegate is async, so this is an async void in all but name - same reasoning as
			// HandleFlyoutPropertyChanged above.
			Gtk.Application.Invoke(async delegate
			{
				try
				{
					await UpdateHamburguerIconAsync();
					if (Page.Flyout != _currentFlyout)
					{
						if (_currentFlyout != null)
						{
							_currentFlyout.PropertyChanged -= HandleFlyoutPropertyChanged;
						}
						if (Platform.GetRenderer(Page.Flyout) == null)
							Platform.SetRenderer(Page.Flyout, Platform.CreateRenderer(Page.Flyout));
						Widget.Flyout = Platform.GetRenderer(Page.Flyout).Container;
						Widget.FlyoutTitle = Page.Flyout?.Title ?? string.Empty;
						Page.Flyout.PropertyChanged += HandleFlyoutPropertyChanged;
						_currentFlyout = Page.Flyout;
					}
					if (Page.Detail != _currentDetail)
					{
						if (Platform.GetRenderer(Page.Detail) == null)
							Platform.SetRenderer(Page.Detail, Platform.CreateRenderer(Page.Detail));
						Widget.Detail = Platform.GetRenderer(Page.Detail).Container;
						_currentDetail = Page.Detail;
					}
					UpdateBarTextColor();
					UpdateBarBackgroundColor();
				}
				catch (Exception ex)
				{
					Internals.Log.Warning("FlyoutPageRenderer", "Could not update the flyout page: {0}", ex);
				}
			});
		}

		private void UpdateIsPresented()
		{
			Widget.IsPresented = Page.IsPresented;
		}

		private void UpdateFlyoutLayoutBehavior()
		{
			// ShouldShowSplitMode is Core's own answer to "is the flyout permanently on screen",
			// and it accounts for idiom and orientation as well as FlyoutLayoutBehavior - which
			// the raw FlyoutLayoutBehavior enum does not. UpdateFlyoutPageBounds already derives
			// the Forms-side rectangles from it, so the native widget has to follow the same
			// authority or the two halves disagree. They did: with a real screen size reported
			// (M4's GtkDeviceInfo), a landscape desktop made Core lay the detail out at
			// x=300 w=500 while the native side still overlaid it full-width at x=0.
			if (Element is IFlyoutPageController controller && controller.ShouldShowSplitMode)
			{
				Widget.FlyoutLayoutBehaviorType = FlyoutLayoutBehaviorType.Split;
			}
			else if (Page.Detail is NavigationPage)
			{
				Widget.FlyoutLayoutBehaviorType = GetFlyoutLayoutBehavior(Page.FlyoutLayoutBehavior);
			}
			else
			{
				// The only way to display Flyout page is from a toolbar. If we have not access to one,
				// we should force split mode to display menu (as no gestures are implemented).
				Widget.FlyoutLayoutBehaviorType = FlyoutLayoutBehaviorType.Split;
			}

			Widget.DisplayTitle = Widget.FlyoutLayoutBehaviorType != FlyoutLayoutBehaviorType.Split;
		}

		private void UpdateBarTextColor()
		{
			var navigationPage = Page.Detail as NavigationPage;

			if (navigationPage != null)
			{
				var barTextColor = navigationPage.BarTextColor;

				Widget.UpdateBarTextColor(barTextColor.ToGtkColor());
			}
		}

		private void UpdateBarBackgroundColor()
		{
			var navigationPage = Page.Detail as NavigationPage;

			if (navigationPage != null)
			{
				var barBackgroundColor = navigationPage.BarBackgroundColor;
				Widget.UpdateBarBackgroundColor(barBackgroundColor.ToGtkColor());
			}
		}

		private Task UpdateHamburguerIconAsync()
		{
			return Page.Flyout.ApplyNativeImageAsync(Xamarin.Forms.Page.IconImageSourceProperty, image =>
			{
				Widget.UpdateHamburguerIcon(image);

				if (Page.Detail is NavigationPage navigationPage)
				{
					var navigationRenderer = Platform.GetRenderer(navigationPage) as IToolbarTracker;
					navigationRenderer?.NativeToolbarTracker.UpdateToolBar();
				}
			});
		}

		private FlyoutLayoutBehaviorType GetFlyoutLayoutBehavior(FlyoutLayoutBehavior flyoutBehavior)
		{
			switch (flyoutBehavior)
			{
				case FlyoutLayoutBehavior.Split:
				case FlyoutLayoutBehavior.SplitOnLandscape:
				case FlyoutLayoutBehavior.SplitOnPortrait:
					return FlyoutLayoutBehaviorType.Split;
				case FlyoutLayoutBehavior.Popover:
					return FlyoutLayoutBehaviorType.Popover;
				case FlyoutLayoutBehavior.Default:
					return FlyoutLayoutBehaviorType.Default;
				default:
					throw new ArgumentOutOfRangeException(nameof(flyoutBehavior));
			}
		}

		private void OnIsPresentedChanged(object sender, EventArgs e)
		{
			ElementController.SetValueFromRenderer(FlyoutPage.IsPresentedProperty, Widget.IsPresented);
		}
	}

	public class MasterDetailPageRenderer : FlyoutPageRenderer
	{

	}
}
