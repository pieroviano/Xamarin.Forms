using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Platform.GTK.Extensions;

namespace Xamarin.Forms.Platform.GTK.Renderers
{
	/// <summary>
	/// SwipeView on GTK: the content is laid out by the inherited layout path (SwipeView is a
	/// ContentView, i.e. a Layout) and the swipe items live in a sibling panel that the content
	/// slides off to reveal. A <see cref="Gtk.GestureDrag"/> on the container drives the slide.
	/// </summary>
	/// <remarks>
	/// The items panel is hosted in a <see cref="Gtk.EventBox"/> with a real window. That is not
	/// cosmetic: a no-window widget is painted into its parent's window, which puts it *under*
	/// the content renderer's container no matter what the child order says (see the RefreshView
	/// note in the plan, §8.1).
	///
	/// Both axes are supported, but only one set of items can be open at a time - the drag's
	/// dominant axis picks which.
	/// </remarks>
	public class SwipeViewRenderer : LayoutRenderer
	{
		// Fraction of the revealed extent the drag must pass to settle open rather than closed.
		const double SettleThreshold = 0.5;
		const int DefaultItemExtent = 80;

		Gtk.EventBox _itemsHost;
		Gtk.Box _itemsBox;
		Gtk.GestureDrag _dragGesture;

		SwipeDirection _activeDirection;
		SwipeItems _activeItems;
		bool _isOpen;
		double _currentOffset;
		bool _swipeStartedSent;

		SwipeView SwipeView => Element as SwipeView;

		ISwipeViewController Controller => Element as ISwipeViewController;

		protected override void OnElementChanged(ElementChangedEventArgs<Layout> e)
		{
			if (e.OldElement is SwipeView oldSwipeView)
			{
				oldSwipeView.OpenRequested -= OnOpenRequested;
				oldSwipeView.CloseRequested -= OnCloseRequested;
			}

			base.OnElementChanged(e);

			if (e.NewElement == null || Control == null)
				return;

			if (_itemsHost == null)
			{
				_itemsBox = new Gtk.Box(Gtk.Orientation.Horizontal, 0);

				_itemsHost = new Gtk.EventBox { VisibleWindow = true };
				_itemsHost.NoShowAll = true;
				_itemsHost.Add(_itemsBox);

				Control.Add(_itemsHost);

				// AddController, not a widget argument to the constructor: Gtk 4 gestures are
				// constructed unattached and then handed to a widget, which is what lets several
				// of them watch the same widget in different propagation phases.
				_dragGesture = new Gtk.GestureDrag();
				Control.AddController(_dragGesture);
				_dragGesture.DragStarted += OnDragBegin;
				_dragGesture.DragUpdate += OnDragUpdate;
				_dragGesture.DragEnded += OnDragEnd;
			}

			if (e.NewElement is SwipeView swipeView)
			{
				swipeView.OpenRequested += OnOpenRequested;
				swipeView.CloseRequested += OnCloseRequested;
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName == SwipeView.LeftItemsProperty.PropertyName ||
				e.PropertyName == SwipeView.RightItemsProperty.PropertyName ||
				e.PropertyName == SwipeView.TopItemsProperty.PropertyName ||
				e.PropertyName == SwipeView.BottomItemsProperty.PropertyName)
			{
				// The open set may have just been replaced underneath us.
				if (_isOpen)
					CloseSwipe();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (Element is SwipeView swipeView)
				{
					swipeView.OpenRequested -= OnOpenRequested;
					swipeView.CloseRequested -= OnCloseRequested;
				}

				if (_dragGesture != null)
				{
					_dragGesture.DragStarted -= OnDragBegin;
					_dragGesture.DragUpdate -= OnDragUpdate;
					_dragGesture.DragEnded -= OnDragEnd;
					_dragGesture.Dispose();
					_dragGesture = null;
				}

				_itemsHost = null;
				_itemsBox = null;
			}

			base.Dispose(disposing);
		}

		// ---- gesture ---------------------------------------------------------------------

		void OnDragBegin(object o, Gtk.DragStartedArgs args)
		{
			_swipeStartedSent = false;
		}

		void OnDragUpdate(object o, Gtk.DragUpdateArgs args)
		{
			if (!_dragGesture.GetOffset(out var offsetX, out var offsetY))
				return;

			var direction = GetDirection(offsetX, offsetY);

			if (direction == null)
				return;

			if (!_swipeStartedSent)
			{
				if (!TryBeginSwipe(direction.Value))
					return;

				_swipeStartedSent = true;
			}
			else if (direction.Value != _activeDirection)
			{
				// Reversing across the axis mid-drag: treat it as a fresh swipe.
				if (!TryBeginSwipe(direction.Value))
					return;
			}

			var travelled = IsHorizontal(_activeDirection) ? Math.Abs(offsetX) : Math.Abs(offsetY);

			_currentOffset = Math.Min(travelled, GetItemsExtent());

			ApplyOffset(_currentOffset);

			Controller?.SendSwipeChanging(new SwipeChangingEventArgs(_activeDirection, _currentOffset));
		}

		void OnDragEnd(object o, Gtk.DragEndedArgs args)
		{
			if (!_swipeStartedSent)
				return;

			_swipeStartedSent = false;

			var extent = GetItemsExtent();
			var settleOpen = extent > 0 && _currentOffset >= extent * SettleThreshold;

			if (settleOpen)
				OpenSwipe();
			else
				CloseSwipe();

			Controller?.SendSwipeEnded(new SwipeEndedEventArgs(_activeDirection, settleOpen));
		}

		bool TryBeginSwipe(SwipeDirection direction)
		{
			var items = GetItems(direction);

			if (items == null || items.Count == 0)
				return false;

			_activeDirection = direction;
			_activeItems = items;

			BuildItems(items, direction);

			Controller?.SendSwipeStarted(new SwipeStartedEventArgs(direction));

			return true;
		}

		// ---- items -----------------------------------------------------------------------

		SwipeItems GetItems(SwipeDirection direction)
		{
			var view = SwipeView;

			if (view == null)
				return null;

			switch (direction)
			{
				// Swiping left reveals the items on the RIGHT, and vice versa.
				case SwipeDirection.Left: return view.RightItems;
				case SwipeDirection.Right: return view.LeftItems;
				case SwipeDirection.Up: return view.BottomItems;
				case SwipeDirection.Down: return view.TopItems;
				default: return null;
			}
		}

		void BuildItems(SwipeItems items, SwipeDirection direction)
		{
			foreach (var child in _itemsBox.Children.ToList())
			{
				_itemsBox.Remove(child);
				child.Destroy();
			}

			_itemsBox.Orientation = IsHorizontal(direction)
				? Gtk.Orientation.Horizontal
				: Gtk.Orientation.Vertical;

			foreach (var item in items.Where(i => i.IsVisible))
				_itemsBox.PackStart(CreateItemWidget(item, items), true, true, 0);

			_itemsBox.ShowAll();
		}

		Gtk.Widget CreateItemWidget(ISwipeItem item, SwipeItems owner)
		{
			var button = new Gtk.Button();

			if (item is SwipeItem swipeItem)
			{
				button.Label = swipeItem.Text ?? string.Empty;

				if (swipeItem.BackgroundColor != Color.Default)
				{
					// GTK3 themes paint a background-image gradient on buttons, which covers
					// background-color: without clearing it the swipe items come out theme-grey
					// however the Forms colour is set.
					button.SetBackgroundColor(swipeItem.BackgroundColor.ToGtkColor());
					button.SetStyleProperty("background-image", "none");
				}
			}
			else
			{
				// SwipeItemView carries arbitrary Forms content. Rendering that content inside the
				// button would need a nested renderer; for now it is a plain activatable surface.
				button.Label = string.Empty;
			}

			button.Clicked += (s, e) => InvokeItem(item, owner);

			return button;
		}

		void InvokeItem(ISwipeItem item, SwipeItems owner)
		{
			item.OnInvoked();

			if (item.Command?.CanExecute(item.CommandParameter) == true)
				item.Command.Execute(item.CommandParameter);

			// Auto: Reveal closes after invoking, Execute stays open.
			var behavior = owner.SwipeBehaviorOnInvoked;

			var shouldClose =
				behavior == SwipeBehaviorOnInvoked.Close ||
				(behavior == SwipeBehaviorOnInvoked.Auto && owner.Mode == SwipeMode.Reveal);

			if (shouldClose)
				CloseSwipe();
		}

		// ---- geometry --------------------------------------------------------------------

		static bool IsHorizontal(SwipeDirection direction) =>
			direction == SwipeDirection.Left || direction == SwipeDirection.Right;

		static SwipeDirection? GetDirection(double offsetX, double offsetY)
		{
			// The dominant axis wins; a drag too small to have a direction is ignored.
			if (Math.Abs(offsetX) < 1 && Math.Abs(offsetY) < 1)
				return null;

			if (Math.Abs(offsetX) >= Math.Abs(offsetY))
				return offsetX < 0 ? SwipeDirection.Left : SwipeDirection.Right;

			return offsetY < 0 ? SwipeDirection.Up : SwipeDirection.Down;
		}

		double GetItemsExtent()
		{
			if (_activeItems == null)
				return 0;

			var visible = _activeItems.Count(i => i.IsVisible);

			if (visible == 0)
				return 0;

			// SwipeItems.Mode/Threshold do not carry a per-item size, so fall back to a fixed
			// extent per item - the same shape the other backends use for their defaults.
			var threshold = SwipeView?.Threshold ?? 0;

			return threshold > 0 ? threshold : visible * DefaultItemExtent;
		}

		void ApplyOffset(double offset)
		{
			if (_itemsHost == null || Control == null)
				return;

			var width = Control.Width;
			var height = Control.Height;
			var extent = (int)Math.Round(offset);

			int hostX, hostY, hostW, hostH;

			switch (_activeDirection)
			{
				case SwipeDirection.Left:   // items sit against the right edge
					hostW = extent; hostH = height; hostX = width - extent; hostY = 0;
					break;
				case SwipeDirection.Right:
					hostW = extent; hostH = height; hostX = 0; hostY = 0;
					break;
				case SwipeDirection.Up:     // items sit against the bottom edge
					hostW = width; hostH = extent; hostX = 0; hostY = height - extent;
					break;
				default:                    // Down
					hostW = width; hostH = extent; hostX = 0; hostY = 0;
					break;
			}

			if (extent <= 0)
			{
				_itemsHost.Visible = false;
				return;
			}

			_itemsHost.SetSizeRequest(Math.Max(1, hostW), Math.Max(1, hostH));

			// Position first, then show: showing a widget allocates it afresh, whereas moving an
			// already-visible Gtk.Fixed child can leave the old allocation in place.
			_itemsHost.MoveTo(hostX, hostY);
			_itemsHost.Visible = true;
			_itemsHost.Raise();
		}

		void OpenSwipe()
		{
			_currentOffset = GetItemsExtent();

			ApplyOffset(_currentOffset);

			_isOpen = true;

			if (Controller != null)
				Controller.IsOpen = true;
		}

		void CloseSwipe()
		{
			_currentOffset = 0;

			if (_itemsHost != null)
				_itemsHost.Visible = false;

			_isOpen = false;

			if (Controller != null)
				Controller.IsOpen = false;
		}

		// ---- Open()/Close() from the element ---------------------------------------------

		void OnOpenRequested(object sender, OpenRequestedEventArgs e)
		{
			var direction = ToDirection(e.OpenSwipeItem);

			if (TryBeginSwipe(direction))
				OpenSwipe();
		}

		void OnCloseRequested(object sender, CloseRequestedEventArgs e)
		{
			CloseSwipe();
		}

		static SwipeDirection ToDirection(OpenSwipeItem openSwipeItem)
		{
			switch (openSwipeItem)
			{
				// OpenSwipeItem names the item set; GetItems maps a swipe direction back to it,
				// so these are inverted on purpose.
				case OpenSwipeItem.LeftItems: return SwipeDirection.Right;
				case OpenSwipeItem.TopItems: return SwipeDirection.Down;
				case OpenSwipeItem.RightItems: return SwipeDirection.Left;
				default: return SwipeDirection.Up;
			}
		}
	}
}
