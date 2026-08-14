using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// CarouselView on GTK: one item visible at a time inside a <see cref="Gtk.Fixed"/> viewport,
	/// with a horizontal drag to move between items.
	/// </summary>
	/// <remarks>
	/// Only the current item is realized. That is a deliberate limitation, not an oversight:
	/// realizing neighbours is what <c>PeekAreaInsets</c> and the sliding animation would need,
	/// and both are unimplemented here (see the plan, §8.1). Materializing every item up front
	/// would also make a large <c>ItemsSource</c> pathological, which is the trap the plan calls
	/// out for CarouselView and CollectionView alike.
	///
	/// Item views are created from <c>ItemTemplate</c>, parented to the CarouselView so the
	/// element chain and binding context inherit correctly, then laid out explicitly - nothing
	/// else lays them out, because they are not children of a Forms Layout.
	/// </remarks>
	public class CarouselViewRenderer : ViewRenderer<CarouselView, Gtk.Fixed>
	{
		// How far a horizontal drag must travel to count as "next"/"previous".
		const int SwipeThreshold = 40;

		readonly List<object> _items = new List<object>();

		View _currentView;
		IVisualElementRenderer _currentRenderer;
		INotifyCollectionChanged _observableSource;
		Gtk.GestureDrag _dragGesture;
		bool _layoutQueued;
		bool _updatingPosition;

		protected override void OnElementChanged(ElementChangedEventArgs<CarouselView> e)
		{
			if (e.OldElement != null)
				UnsubscribeSource();

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					SetNativeControl(new Gtk.Fixed());

					// AddController, not a widget argument to the constructor: Gtk 4 gestures are
					// constructed unattached and then handed to a widget, which is what lets several
					// of them watch the same widget in different propagation phases.
					_dragGesture = new Gtk.GestureDrag();
					Control.AddController(_dragGesture);
					_dragGesture.DragEnded += OnDragEnd;
				}

				SubscribeSource();
				ReloadItems();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == ItemsView.ItemsSourceProperty.PropertyName)
			{
				UnsubscribeSource();
				SubscribeSource();
				ReloadItems();
			}
			else if (e.PropertyName == StructuredItemsView.ItemTemplateProperty.PropertyName)
			{
				ReloadItems();
			}
			else if (e.PropertyName == CarouselView.PositionProperty.PropertyName)
			{
				if (!_updatingPosition)
					ShowCurrent();
			}
			else if (e.PropertyName == CarouselView.CurrentItemProperty.PropertyName)
			{
				SyncPositionToCurrentItem();
			}
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			// Deferred: laying the item out mutates geometry, and GTK3 discards that when it
			// happens inside a size-allocate.
			if (_layoutQueued)
				return;

			_layoutQueued = true;

			GLib.Idle.Add(() =>
			{
				_layoutQueued = false;
				LayoutCurrentView();
				return false;
			});
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				UnsubscribeSource();
				DestroyCurrentView();

				if (_dragGesture != null)
				{
					_dragGesture.DragEnded -= OnDragEnd;
					_dragGesture.Dispose();
					_dragGesture = null;
				}
			}

			base.Dispose(disposing);
		}

		// ---- items -----------------------------------------------------------------------

		void SubscribeSource()
		{
			_observableSource = Element?.ItemsSource as INotifyCollectionChanged;

			if (_observableSource != null)
				_observableSource.CollectionChanged += OnSourceCollectionChanged;
		}

		void UnsubscribeSource()
		{
			if (_observableSource != null)
			{
				_observableSource.CollectionChanged -= OnSourceCollectionChanged;
				_observableSource = null;
			}
		}

		void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			ReloadItems();
		}

		void ReloadItems()
		{
			_items.Clear();

			if (Element?.ItemsSource is IEnumerable source)
				_items.AddRange(source.Cast<object>());

			// Keep Position inside the new bounds.
			var position = Element == null ? 0 : Element.Position;

			if (position >= _items.Count)
				position = Math.Max(0, _items.Count - 1);

			SetPosition(position);
			ShowCurrent();
		}

		void ShowCurrent()
		{
			DestroyCurrentView();

			if (Element == null || _items.Count == 0)
				return;

			var position = Math.Max(0, Math.Min(Element.Position, _items.Count - 1));
			var item = _items[position];

			var template = Element.ItemTemplate;

			if (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(item, Element);

			if (template == null)
				return;

			if (!(template.CreateContent() is View view))
				return;

			// Parent first, then BindingContext: parenting wires the element chain (and would
			// otherwise overwrite the binding context by inheriting the carousel's).
			view.Parent = Element;
			view.BindingContext = item;

			_currentView = view;
			_currentRenderer = Platform.CreateRenderer(view);
			Platform.SetRenderer(view, _currentRenderer);

			Control.Add(_currentRenderer.Container);
			_currentRenderer.Container.ShowAll();

			LayoutCurrentView();

			// Keep CurrentItem in step with what is on screen.
			if (!Equals(Element.CurrentItem, item))
				Element.SetValueFromRenderer(CarouselView.CurrentItemProperty, item);
		}

		void LayoutCurrentView()
		{
			if (_currentView == null || Control == null)
				return;

			var width = Control.Width;
			var height = Control.Height;

			if (width <= 1 || height <= 1)
				return;

			// Nothing else lays these views out - they are not children of a Forms Layout - so
			// the renderer has to run the Forms layout pass itself.
			_currentView.Layout(new Rectangle(0, 0, width, height));

			_currentRenderer?.Container?.SetSize(width, height);
			_currentRenderer?.Container?.MoveTo(0, 0);
		}

		void DestroyCurrentView()
		{
			if (_currentRenderer != null)
			{
				Control?.RemoveFromContainer(_currentRenderer.Container);
				_currentRenderer.Dispose();
				_currentRenderer = null;
			}

			if (_currentView != null)
			{
				Platform.SetRenderer(_currentView, null);
				_currentView.Parent = null;
				_currentView = null;
			}
		}

		// ---- position --------------------------------------------------------------------

		void SetPosition(int position)
		{
			if (Element == null || Element.Position == position)
				return;

			// Guard so the property-changed handler does not re-enter ShowCurrent while we are
			// already rebuilding for this very position.
			_updatingPosition = true;

			try
			{
				Element.SetValueFromRenderer(CarouselView.PositionProperty, position);
			}
			finally
			{
				_updatingPosition = false;
			}
		}

		void SyncPositionToCurrentItem()
		{
			if (Element?.CurrentItem == null)
				return;

			var index = _items.IndexOf(Element.CurrentItem);

			if (index < 0 || index == Element.Position)
				return;

			SetPosition(index);
			ShowCurrent();
		}

		void OnDragEnd(object o, Gtk.DragEndedArgs args)
		{
			if (Element == null || !Element.IsSwipeEnabled || _items.Count == 0)
				return;

			if (!_dragGesture.GetOffset(out var offsetX, out var offsetY))
				return;

			// Horizontal drags only; a mostly-vertical drag is not a carousel swipe.
			if (Math.Abs(offsetX) < SwipeThreshold || Math.Abs(offsetX) <= Math.Abs(offsetY))
				return;

			var delta = offsetX < 0 ? 1 : -1;   // drag left => next
			var next = Element.Position + delta;

			if (next < 0 || next >= _items.Count)
			{
				if (!Element.Loop)
					return;

				next = next < 0 ? _items.Count - 1 : 0;
			}

			SetPosition(next);
			ShowCurrent();
		}
	}
}
