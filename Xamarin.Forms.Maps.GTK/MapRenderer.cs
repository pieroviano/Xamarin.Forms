using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xamarin.Forms.Maps.GTK.Controls;
using Xamarin.Forms.Platform.GTK;

namespace Xamarin.Forms.Maps.GTK
{
	public class MapRenderer : ViewRenderer<Map, MapControl>
	{
		bool _disposed;
		uint _userPositionTimer;
		int _lastWidth;
		int _lastHeight;

		protected override void OnElementChanged(ElementChangedEventArgs<Map> e)
		{
			if (e.OldElement != null)
			{
				MessagingCenter.Unsubscribe<Map, MapSpan>(this, "MapMoveToRegion");

				UnhookCollections(e.OldElement);
			}

			if (e.NewElement != null)
			{
				if (Control == null)
				{
					var mapControl = new MapControl();

					SetNativeControl(mapControl);

					Control.MapClicked += OnMapClicked;
					Control.PinClicked += OnPinClicked;
					Control.ViewportChanged += OnViewportChanged;
					Control.SizeAllocated += OnControlSizeAllocated;
				}

				MessagingCenter.Subscribe<Map, MapSpan>(this, "MapMoveToRegion", (s, span) => Control?.MoveToRegion(span), e.NewElement);

				HookCollections(e.NewElement);

				UpdateMapType();
				UpdateHasScrollEnabled();
				UpdateHasZoomEnabled();

				LoadPins();
				LoadElements();

				Control.MoveToRegion(e.NewElement.LastMoveToRegion);

				UpdateIsShowingUser();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			// Xamarin.Forms.Maps.Map has to be spelled out: inside a renderer the bare name Map
			// binds to the inherited Gtk.Widget.Map() method, not to the Forms type (CS0119).
			if (e.PropertyName == Xamarin.Forms.Maps.Map.MapTypeProperty.PropertyName)
				UpdateMapType();
			else if (e.PropertyName == Xamarin.Forms.Maps.Map.IsShowingUserProperty.PropertyName)
				UpdateIsShowingUser();
			else if (e.PropertyName == Xamarin.Forms.Maps.Map.HasScrollEnabledProperty.PropertyName)
				UpdateHasScrollEnabled();
			else if (e.PropertyName == Xamarin.Forms.Maps.Map.HasZoomEnabledProperty.PropertyName)
				UpdateHasZoomEnabled();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_disposed)
			{
				_disposed = true;

				MessagingCenter.Unsubscribe<Map, MapSpan>(this, "MapMoveToRegion");

				StopUserPositionTimer();

				if (Control != null)
				{
					Control.MapClicked -= OnMapClicked;
					Control.PinClicked -= OnPinClicked;
					Control.ViewportChanged -= OnViewportChanged;
					Control.SizeAllocated -= OnControlSizeAllocated;
				}

				if (Element != null)
					UnhookCollections(Element);
			}

			base.Dispose(disposing);
		}

		#region Collection plumbing

		void HookCollections(Map map)
		{
			((ObservableCollection<Pin>)map.Pins).CollectionChanged += OnPinsChanged;
			((ObservableCollection<MapElement>)map.MapElements).CollectionChanged += OnElementsChanged;
		}

		void UnhookCollections(Map map)
		{
			((ObservableCollection<Pin>)map.Pins).CollectionChanged -= OnPinsChanged;
			((ObservableCollection<MapElement>)map.MapElements).CollectionChanged -= OnElementsChanged;

			foreach (var pin in map.Pins)
				pin.PropertyChanged -= OnPinPropertyChanged;

			foreach (var element in map.MapElements)
				element.PropertyChanged -= OnMapElementPropertyChanged;
		}

		void OnPinsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (Pin pin in e.OldItems)
					pin.PropertyChanged -= OnPinPropertyChanged;
			}

			LoadPins();
		}

		void OnElementsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (MapElement element in e.OldItems)
					element.PropertyChanged -= OnMapElementPropertyChanged;
			}

			LoadElements();
		}

		void OnPinPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Control?.Invalidate();
		}

		void OnMapElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Control?.Invalidate();
		}

		/// <summary>
		/// Rebuilds the control's pin list wholesale. There are rarely more than a few dozen pins,
		/// and drawing is immediate-mode, so a full resync is cheaper than tracking deltas - and it
		/// cannot drift out of step with <see cref="Map.Pins"/> the way the old add/remove-by-value
		/// implementation did (it removed *every* pin sharing a position).
		/// </summary>
		void LoadPins()
		{
			if (Control == null || Element == null)
				return;

			foreach (var pin in Control.Pins)
				pin.PropertyChanged -= OnPinPropertyChanged;

			Control.Pins.Clear();

			foreach (var pin in Element.Pins)
			{
				pin.PropertyChanged += OnPinPropertyChanged;
				Control.Pins.Add(pin);
			}

			Control.Invalidate();
		}

		void LoadElements()
		{
			if (Control == null || Element == null)
				return;

			foreach (var element in Control.Elements)
				element.PropertyChanged -= OnMapElementPropertyChanged;

			Control.Elements.Clear();

			foreach (var element in Element.MapElements)
			{
				element.PropertyChanged += OnMapElementPropertyChanged;
				Control.Elements.Add(element);
			}

			Control.Invalidate();
		}

		#endregion

		#region Property updates

		void UpdateMapType()
		{
			if (Control == null || Element == null)
				return;

			Control.TileSource = FormsMaps.ResolveTileSource(Element.MapType);
		}

		void UpdateHasScrollEnabled()
		{
			if (Control == null || Element == null)
				return;

			Control.PanEnabled = Element.HasScrollEnabled;
		}

		void UpdateHasZoomEnabled()
		{
			if (Control == null || Element == null)
				return;

			Control.ZoomEnabled = Element.HasZoomEnabled;
		}

		/// <summary>
		/// GTK has no location service. <see cref="FormsMaps.UserPositionProvider"/> is the seam an
		/// application plugs its own into; without one, <see cref="Map.IsShowingUser"/> draws
		/// nothing (documented in the README as a known limitation).
		/// </summary>
		void UpdateIsShowingUser()
		{
			if (Control == null || Element == null)
				return;

			if (!Element.IsShowingUser)
			{
				StopUserPositionTimer();
				Control.UserPosition = null;

				return;
			}

			if (FormsMaps.UserPositionProvider == null)
				return;

			RefreshUserPosition();

			if (_userPositionTimer != 0)
				return;

			var interval = (uint)Math.Max(1000, FormsMaps.UserPositionRefreshInterval.TotalMilliseconds);

			_userPositionTimer = GLib.Timeout.Add(interval, () =>
			{
				if (_disposed || Element == null || !Element.IsShowingUser)
				{
					_userPositionTimer = 0;
					return false;
				}

				RefreshUserPosition();

				return true;
			});
		}

		void RefreshUserPosition()
		{
			var provider = FormsMaps.UserPositionProvider;

			if (provider == null)
				return;

			provider().ContinueWith(task =>
			{
				var position = task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? task.Result : null;

				GLib.Idle.Add(() =>
				{
					if (!_disposed && Control != null)
						Control.UserPosition = position;

					return false;
				});
			}, System.Threading.Tasks.TaskScheduler.Default);
		}

		void StopUserPositionTimer()
		{
			if (_userPositionTimer == 0)
				return;

			GLib.Source.Remove(_userPositionTimer);
			_userPositionTimer = 0;
		}

		#endregion

		#region Control events

		void OnViewportChanged(object sender, EventArgs e)
		{
			if (_disposed || Control == null || Element == null)
				return;

			Element.SetVisibleRegion(Control.VisibleRegion);
		}

		void OnControlSizeAllocated(object o, Gtk.SizeAllocatedArgs args)
		{
			if (_disposed || Control == null || Element == null)
				return;

			// args.Allocation, not args.Width/Height: the compat SizeAllocatedArgs carries the
			// rectangle the Gtk 3 signal carried, and nothing else - Gtk 4 has no size-allocate
			// signal at all, only the vfunc the shim raises this from.
			var width = args.Allocation.Width;
			var height = args.Allocation.Height;

			if (width == _lastWidth && height == _lastHeight)
				return;

			_lastWidth = width;
			_lastHeight = height;

			if (!Element.MoveToLastRegionOnLayoutChange)
				return;

			var region = Element.LastMoveToRegion;

			if (region == null)
				return;

			// Never re-fit inside the size-allocate itself: GTK 3 discards work done there, and the
			// fit depends on the allocation that is only just being applied.
			GLib.Idle.Add(() =>
			{
				if (!_disposed && Control != null)
					Control.MoveToRegion(region);

				return false;
			});
		}

		void OnMapClicked(object sender, MapPositionEventArgs e)
		{
			Element?.SendMapClicked(e.Position);
		}

		void OnPinClicked(object sender, MapPinEventArgs e)
		{
			if (Element == null || e.Pin == null)
				return;

#pragma warning disable CS0618 // Pin.SendTap is obsolete; still raised so pre-4.3 Clicked handlers keep working.
			e.Pin.SendTap();
#pragma warning restore CS0618

			e.Pin.SendMarkerClick();
		}

		#endregion
	}
}
