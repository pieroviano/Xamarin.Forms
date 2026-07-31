using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Gtk;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.GTK.Extensions;
using Container = Xamarin.Forms.Platform.GTK.GtkFormsContainer;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	public abstract class AbstractPageRenderer<TWidget, TPage> : Container, IPageControl, IVisualElementRenderer, IEffectControlProvider
		where TWidget : Widget
		where TPage : Page
	{
		private Gdk.Rectangle _lastAllocation;
		private DateTime _lastAllocationTime;
		protected bool _disposed;
		protected bool _appeared;
		protected readonly PropertyChangedEventHandler _propertyChangedHandler;

		protected AbstractPageRenderer()
		{
			VisibleWindow = true;
			_propertyChangedHandler = OnElementPropertyChanged;
		}

		public Controls.Page Control { get; protected set; }

		public TWidget Widget { get; protected set; }

		public VisualElement Element { get; protected set; }

		public TPage Page => Element as TPage;

		public bool Disposed { get { return _disposed; } }

		public Container Container => this;

		public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

		protected IElementController ElementController => Element as IElementController;

		protected IPageController PageController => Element as IPageController;

		void IEffectControlProvider.RegisterEffect(Effect effect)
		{
			var platformEffect = effect as PlatformEffect;
			if (platformEffect != null)
				platformEffect.SetContainer(Container);
		}

		public virtual void SetElement(VisualElement element)
		{
			VisualElement oldElement = Element;
			Element = element;

			OnElementChanged(new VisualElementChangedEventArgs(oldElement, element));

			EffectUtilities.RegisterEffectControlProvider(this, oldElement, element);
		}

		public virtual void SetElementSize(Size newSize)
		{
			if (Element == null)
				return;

			var elementSize = new Size(Element.Bounds.Width, Element.Bounds.Height);

			if (elementSize == newSize)
				return;

			var bounds = new Rectangle(Element.X, Element.Y, newSize.Width, newSize.Height);

			Element.Layout(bounds);
		}

		public SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			return Container.GetDesiredSize(widthConstraint, heightConstraint);
		}

		public override void Destroy()
		{
			base.Destroy();

			if (!_disposed)
			{
				if (_appeared)
				{
					ReadOnlyCollection<Element> children = ((IElementController)Element).LogicalChildren;
					for (var i = 0; i < children.Count; i++)
					{
						var visualChild = children[i] as VisualElement;
						visualChild?.Cleanup();
					}

					Page.SendDisappearing();
				}

				_appeared = false;

				Dispose(true);

				_disposed = true;
			}
		}

		protected override void OnShown()
		{
			base.OnShown();

			if (_appeared || _disposed)
				return;

			UpdateBackgroundColor();
			UpdateBackgroundImage();

			_appeared = true;

			PageController.SendAppearing();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			var now = DateTime.Now;
			var diff = now.Subtract(_lastAllocationTime);

			if (_lastAllocation != allocation)
			{
				_lastAllocation = allocation;
				_lastAllocationTime = now;
				SetPageSize(_lastAllocation.Width, _lastAllocation.Height); // Check ToolBar for size calculations.
				PageQueueResize();
			}
			else if (diff > TimeSpan.FromMilliseconds(50)) // Prevent infinite resizing loops for very fast layout changes
			{
				SetPageSize(allocation.Width, allocation.Height);
			}
		}

		// GtkSharp 3 introduces Widget.Dispose(bool); override it so disposal chains
		// through GTK instead of shadowing it.
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Element != null)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
				}

				Platform.SetRenderer(Element, null);

				Control?.Destroy();
				Control = null;
				Element = null;
			}

			base.Dispose(disposing);
		}

		protected virtual void OnElementChanged(VisualElementChangedEventArgs e)
		{
			if (e.OldElement != null)
			{
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
				e.OldElement.BatchCommitted -= OnElementBatchCommitted;
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					Control = new Controls.Page();
					Add(Control);
				}

				e.NewElement.PropertyChanged += OnElementPropertyChanged;
				// See UpdateChildrenLayout(): a Forms layout pass does not queue a GTK
				// resize, so the page's content would otherwise keep its natural size.
				e.NewElement.BatchCommitted += OnElementBatchCommitted;
			}

			UpdateBackgroundImage();

			ElementChanged?.Invoke(this, e);
		}

		bool _layoutUpdateQueued;

		void OnElementBatchCommitted(object sender, Internals.EventArg<VisualElement> e)
		{
			// Deferred to idle: Forms raises BatchCommitted from inside the size-allocate
			// cycle, and GTK3 discards resizes queued during allocation. See
			// VisualElementRenderer.QueueLayoutUpdate for the full explanation.
			if (_layoutUpdateQueued)
				return;

			_layoutUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_layoutUpdateQueued = false;
				UpdateChildrenLayout();
				return false;
			});
		}

		/// <summary>
		/// Pushes the Forms-computed geometry of the page's children onto their GTK widgets.
		/// </summary>
		/// <remarks>
		/// This renderer does not derive from <see cref="VisualElementRenderer{TElement,TNativeElement}"/>,
		/// so it needs its own copy of that propagation. Without it the page's content
		/// (typically the root layout) keeps its natural size - a StackLayout that Forms
		/// measured at 500x400 stayed at 182x34 and clipped everything inside it.
		/// </remarks>
		protected virtual void UpdateChildrenLayout()
		{
			var controller = Element as IElementController;

			if (controller == null)
				return;

			for (var i = 0; i < controller.LogicalChildren.Count; i++)
			{
				var child = controller.LogicalChildren[i] as VisualElement;

				if (child == null)
					continue;

				var renderer = Platform.GetRenderer(child);

				if (renderer?.Container == null)
					continue;

				var width = child.Bounds.Width >= -1 ? child.Bounds.Width : 0;
				var height = child.Bounds.Height >= -1 ? child.Bounds.Height : 0;

				renderer.Container.SetSize(width, height);
				renderer.Container.MoveTo(child.Bounds.X + child.TranslationX, child.Bounds.Y + child.TranslationY);
			}
		}

		protected virtual void UpdateBackgroundColor()
		{
			Control.SetBackgroundColor(Element.BackgroundColor);
		}

		protected virtual void UpdateBackgroundImage()
		{
			Control.SetBackgroundImage(Page.BackgroundImageSource);
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName == Xamarin.Forms.Page.BackgroundImageSourceProperty.PropertyName)
				UpdateBackgroundImage();
		}

		protected virtual void SetPageSize(int width, int height)
		{
			var finalHeight = height;

			if (Page != null &&
				HasAncestorNavigationPage(Page))
				finalHeight -= GtkToolbarConstants.ToolbarHeight; // Subtract the size of the Toolbar.

			var pageContentSize = new Gdk.Rectangle(0, 0, width, finalHeight);
			var newSize = pageContentSize.ToSize();

			SetElementSize(newSize);
		}

		bool _contentResizeQueued;

		/// <summary>
		/// Queues a resize of the page content - deferred to idle, never inline.
		/// </summary>
		/// <remarks>
		/// This is called from <see cref="OnSizeAllocated"/>, i.e. from inside GTK's size-allocate
		/// cycle, and GTK3 discards a resize queued from there. Worse than discarding it: the
		/// content's parent keeps its resize-needed flag set for good, and
		/// gtk_widget_queue_resize_internal bails out at the first ancestor that already has it -
		/// so every later queue_resize raised from anywhere in that subtree is swallowed too.
		///
		/// Measured on the ControlGallery's FlyoutPage: with the request raised inline, a
		/// QueueResize on the flyout wrapper, on Controls.FlyoutPage, or on the content container
		/// changed nothing, and only a QueueResize on the *toplevel* re-allocated the subtree. That
		/// is what stopped a Gtk.Revealer's animated width from ever reaching the widget's
		/// allocation (preferred width climbed 56 -> 299 -> 300 while the allocation sat at 1px).
		/// </remarks>
		private void PageQueueResize()
		{
			if (_contentResizeQueued)
				return;

			_contentResizeQueued = true;

			GLib.Idle.Add(() =>
			{
				_contentResizeQueued = false;
				Control?.Content?.QueueResize();

				return false;
			});
		}

		private bool HasAncestorNavigationPage(TPage page)
		{
			bool hasParentNavigation = false;
			TPage parent;
			TPage current = page;

			while ((parent = current.Parent as TPage) != null)
			{
				hasParentNavigation = parent is NavigationPage;

				current = parent;

				if (hasParentNavigation)
					break;
			}
			var hasAncestorNavigationPage = hasParentNavigation && NavigationPage.GetHasNavigationBar(current);
			return hasAncestorNavigationPage;
		}
	}
}
