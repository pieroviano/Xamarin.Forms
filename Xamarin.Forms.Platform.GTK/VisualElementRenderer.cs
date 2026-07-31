using System;
using System.Collections.Generic;
using System.ComponentModel;
using Gtk;
using Xamarin.Forms.Platform.GTK.Extensions;
using Container = Xamarin.Forms.Platform.GTK.GtkFormsContainer;
using Control = Gtk.Widget;

namespace Xamarin.Forms.Platform.GTK
{
	public class VisualElementRenderer<TElement, TNativeElement> : Container, IVisualNativeElementRenderer, IVisualElementRenderer, IDisposable, IEffectControlProvider
		where TElement : VisualElement
		where TNativeElement : Control
	{
		private bool _disposed;
		private readonly PropertyChangedEventHandler _propertyChangedHandler;
		private readonly List<EventHandler<VisualElementChangedEventArgs>> _elementChangedHandlers = new List<EventHandler<VisualElementChangedEventArgs>>();
		private VisualElementTracker<TElement, TNativeElement> _tracker;
		private string _defaultAccessibilityLabel;
		private string _defaultAccessibilityHint;

		protected VisualElementRenderer()
		{
			_propertyChangedHandler = OnElementPropertyChanged;
		}

		protected VisualElementTracker<TElement, TNativeElement> Tracker
		{
			get { return _tracker; }
			set
			{
				if (_tracker == value)
					return;

				if (_tracker != null)
				{
					_tracker.Dispose();
					_tracker.Updated -= OnTrackerUpdated;
				}

				_tracker = value;

				if (_tracker != null)
				{
					_tracker.Updated += OnTrackerUpdated;
					UpdateTracker();
				}
			}
		}

		public TNativeElement Control { get; set; }

		Control IVisualNativeElementRenderer.Control => Control;

		public TElement Element { get; set; }

		public Container Container => this;

		public bool Disposed { get { return _disposed; } }

		VisualElement IVisualElementRenderer.Element
		{
			get
			{
				return Element;
			}
		}

		protected IElementController ElementController => Element as IElementController;

		protected virtual bool PreventGestureBubbling { get; set; } = false;

		event EventHandler<VisualElementChangedEventArgs> IVisualElementRenderer.ElementChanged
		{
			add { _elementChangedHandlers.Add(value); }
			remove { _elementChangedHandlers.Remove(value); }
		}

		public event EventHandler<ElementChangedEventArgs<TElement>> ElementChanged;

		void IEffectControlProvider.RegisterEffect(Effect effect)
		{
			var platformEffect = effect as PlatformEffect;
			if (platformEffect != null)
				OnRegisterEffect(platformEffect);
		}

		void IVisualElementRenderer.SetElement(VisualElement element)
		{
			SetElement((TElement)element);
		}

		public void SetElement(TElement element)
		{
			var oldElement = Element;
			Element = element;

			if (oldElement != null)
			{
				oldElement.FocusChangeRequested -= OnElementFocusChangeRequested;
				oldElement.PropertyChanged -= _propertyChangedHandler;
				oldElement.BatchCommitted -= OnElementBatchCommitted;
			}

			if (element != null)
			{
				element.PropertyChanged += _propertyChangedHandler;
				element.FocusChangeRequested += OnElementFocusChangeRequested;
				// Forms raises this once a layout pass has committed new bounds; it is what
				// gets that geometry onto the GTK widgets. See UpdateElementLayout().
				element.BatchCommitted += OnElementBatchCommitted;

				if (Tracker == null)
				{
					Tracker = new VisualElementTracker<TElement, TNativeElement>();
				}
			}

			OnElementChanged(new ElementChangedEventArgs<TElement>(oldElement, element));

			SetAccessibilityLabel();
			SetAccessibilityHint();
		}

		public void SetElementSize(Size size)
		{
			Layout.LayoutChildIntoBoundingRegion(Element,
				new Rectangle(Element.X, Element.Y, size.Width, size.Height));
		}

		public virtual SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
		{
			if (Children.Length == 0)
				return new SizeRequest();

			return Control.GetDesiredSize(widthConstraint, heightConstraint);
		}

		protected override void OnShown()
		{
			base.OnShown();

			UpdateIsVisible();
		}

		public override void Destroy()
		{
			base.Destroy();
			Dispose(true);
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			UpdateElementLayout();
		}

		/// <summary>
		/// Pushes the Forms-computed geometry of this element (and its logical children)
		/// onto the native GTK widgets.
		/// </summary>
		/// <remarks>
		/// This used to run only from <see cref="OnSizeAllocated"/>, i.e. only when GTK
		/// decided to allocate. That is a chicken-and-egg: inside a <see cref="Gtk.Fixed"/>
		/// a child is allocated at (0,0) at its natural size until something calls
		/// Move/SetSizeRequest, and a Forms layout pass does not itself queue a GTK resize.
		/// The result was that Forms computed correct bounds which never reached GTK, so
		/// every widget stacked at the origin at its natural size.
		///
		/// It is therefore also driven from the element's BatchCommitted (see
		/// <see cref="OnElementBatchCommitted"/>), which Forms raises once a layout pass
		/// has committed the new bounds.
		/// </remarks>
		protected void UpdateElementLayout()
		{
			if (_disposed || Element == null || Container == null)
				return;

			Rectangle bounds = Element.Bounds;

			var width = bounds.Width >= -1 ? bounds.Width : 0;
			var height = bounds.Height >= -1 ? bounds.Height : 0;

			Container.SetSize(width, height);
			Container.MoveTo((int)bounds.X + Element.TranslationX, (int)bounds.Y + Element.TranslationY);

			if (ElementController == null)
				return;

			for (var i = 0; i < ElementController.LogicalChildren.Count; i++)
			{
				var child = ElementController.LogicalChildren[i] as VisualElement;

				if (child == null)
					continue;

				var renderer = Platform.GetRenderer(child);

				if (renderer?.Container == null)
					continue;

				var childWidth = child.Bounds.Width >= -1 ? child.Bounds.Width : 0;
				var childHeight = child.Bounds.Height >= -1 ? child.Bounds.Height : 0;

				renderer.Container.SetSize(childWidth, childHeight);
				renderer.Container.MoveTo(child.Bounds.X + child.TranslationX, child.Bounds.Y + child.TranslationY);
			}
		}

		bool _layoutUpdateQueued;

		void OnElementBatchCommitted(object sender, Internals.EventArg<VisualElement> e)
		{
			QueueLayoutUpdate();
		}

		/// <summary>
		/// Applies the Forms geometry on an idle callback rather than inline.
		/// </summary>
		/// <remarks>
		/// Forms raises BatchCommitted from inside the page's size-allocate cycle, and GTK3
		/// discards resizes queued while an allocation is in progress. Applying the geometry
		/// inline therefore set WidthRequest/HeightRequest correctly but never produced a
		/// second allocation pass, so the widgets kept their natural size on screen.
		/// Deferring to idle lets the current allocation finish first, so the queued resize
		/// is honoured.
		/// </remarks>
		protected void QueueLayoutUpdate()
		{
			if (_layoutUpdateQueued || _disposed)
				return;

			_layoutUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_layoutUpdateQueued = false;

				if (!_disposed)
					UpdateElementLayout();

				return false;
			});
		}

		protected virtual void OnRegisterEffect(PlatformEffect effect)
		{
			effect.SetContainer(this);
			effect.SetControl(Container);
		}

		protected virtual void OnElementChanged(ElementChangedEventArgs<TElement> e)
		{
			var args = new VisualElementChangedEventArgs(e.OldElement, e.NewElement);
			for (var i = 0; i < _elementChangedHandlers.Count; i++)
				_elementChangedHandlers[i](this, args);

			ElementChanged?.Invoke(this, e);
		}

		protected virtual void SetNativeControl(TNativeElement view)
		{
			Control = view;

			UpdateBackgroundColor();
			UpdateIsVisible();
			UpdateSensitive();
		}

		// GtkSharp 3 introduces Widget.Dispose(bool); override it so disposal chains
		// through GTK instead of shadowing it.
		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				Tracker?.Dispose();
				Tracker = null;
			}

			base.Dispose(disposing);
		}

		protected virtual void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName)
				UpdateIsVisible();
			else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				UpdateBackgroundColor();
			else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
				UpdateSensitive();
			else if (e.PropertyName == AutomationProperties.HelpTextProperty.PropertyName)
				SetAccessibilityHint();
			else if (e.PropertyName == AutomationProperties.NameProperty.PropertyName)
				SetAccessibilityLabel();
		}

		protected virtual void UpdateBackgroundColor()
		{
			if (_disposed || Element == null || Control == null)
				return;

			Container.SetBackgroundColor(Element.BackgroundColor);
		}

		protected virtual void SetAccessibilityHint()
		{
			if (_disposed || Element == null || Control == null)
				return;

			if (_defaultAccessibilityHint == null)
				_defaultAccessibilityHint = Accessible.Name;

			var helpText = (string)Element.GetValue(AutomationProperties.HelpTextProperty) ?? _defaultAccessibilityHint;

			if (!string.IsNullOrEmpty(helpText))
			{
				Accessible.Name = helpText;
			}
		}

		protected virtual void SetAccessibilityLabel()
		{
			if (_disposed || Element == null || Control == null)
				return;

			if (_defaultAccessibilityLabel == null)
				_defaultAccessibilityLabel = Accessible.Description;

			var name = (string)Element.GetValue(AutomationProperties.NameProperty) ?? _defaultAccessibilityLabel;

			if (!string.IsNullOrEmpty(name))
			{
				Accessible.Description = name;
			}
		}

		protected virtual void UpdateNativeControl()
		{
			UpdateSensitive();
		}

		internal virtual void OnElementFocusChangeRequested(object sender, VisualElement.FocusRequestArgs args)
		{
			var control = Control as Control;

			if (control == null)
				return;

			if (args.Focus)
				args.Result = control.IsFocus = true;
			else
			{
				control.IsFocus = false;
				args.Result = true;
			}
		}

		private void UpdateIsVisible()
		{
			if (_disposed || Element == null || Control == null)
				return;

			Container.Visible = Element.IsVisible;
		}

		private void UpdateSensitive()
		{
			if (_disposed || Element == null || Control == null)
				return;

			Control.Sensitive = Element.IsEnabled;
		}

		private void OnTrackerUpdated(object sender, EventArgs e)
		{
			UpdateNativeControl();
		}

		private void UpdateTracker()
		{
			if (_tracker == null)
				return;

			_tracker.PreventGestureBubbling = PreventGestureBubbling;
			_tracker.Control = Control;
			_tracker.Element = Element;
			_tracker.Container = Container;
		}
	}
}
