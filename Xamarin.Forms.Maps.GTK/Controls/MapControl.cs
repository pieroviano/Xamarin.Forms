using System;
using System.Collections.Generic;
using System.Linq;

namespace Xamarin.Forms.Maps.GTK.Controls
{
	/// <summary>
	/// A Web-Mercator "slippy map" drawn with Cairo onto a <see cref="Gtk.DrawingArea"/>.
	/// </summary>
	/// <remarks>
	/// This replaces the GTK# 2 / GMap.NET.GTK control the GTK backend used to bind. GMap.NET.GTK
	/// has no GTK 3 successor and renders through System.Drawing, and the obvious native
	/// alternative (libosmgpsmap-1.0) is not installed on a stock GTK desktop - see plan §9.3. The
	/// only dependency here is GTK 3 itself.
	/// </remarks>
	public class MapControl : Gtk.DrawingArea
	{
		const int TileSize = MercatorProjection.TileSize;

		const double PinHeadRadius = 8.0;
		const double PinHeadOffset = 20.0;
		const double PinHitPadding = 4.0;

		const double DefaultStrokeWidth = 3.0;
		const string LabelFont = "Sans 9";
		const string AttributionFont = "Sans 7";

		readonly List<Pin> _pins = new List<Pin>();
		readonly List<MapElement> _elements = new List<MapElement>();
		readonly TileCache _tiles = TileCache.Default;

		double _centerLatitude;
		double _centerLongitude;
		int _zoom = 12;

		MapTileSource _tileSource = MapTileSource.Street;
		MapSpan _pendingRegion;
		Position? _userPosition;

		bool _panEnabled = true;
		bool _zoomEnabled = true;
		bool _dragging;
		bool _dragMoved;
		double _dragLastX;
		double _dragLastY;

		bool _viewportUpdateQueued;
		bool _destroyed;

		public MapControl()
		{
			AddEvents((int)(Gdk.EventMask.ButtonPressMask
				| Gdk.EventMask.ButtonReleaseMask
				| Gdk.EventMask.PointerMotionMask
				| Gdk.EventMask.ScrollMask
				| Gdk.EventMask.SmoothScrollMask));

			CanFocus = true;
		}

		/// <summary>Raised for a click that did not hit a pin and was not a drag.</summary>
		public event EventHandler<MapPositionEventArgs> MapClicked;

		/// <summary>Raised when a pin marker is clicked.</summary>
		public event EventHandler<MapPinEventArgs> PinClicked;

		/// <summary>Raised (coalesced, on the idle loop) whenever the visible region changes.</summary>
		public event EventHandler ViewportChanged;

		public double CenterLatitude => _centerLatitude;

		public double CenterLongitude => _centerLongitude;

		public int Zoom => _zoom;

		public bool PanEnabled
		{
			get => _panEnabled;
			set => _panEnabled = value;
		}

		public bool ZoomEnabled
		{
			get => _zoomEnabled;
			set => _zoomEnabled = value;
		}

		public MapTileSource TileSource
		{
			get => _tileSource;
			set
			{
				value = value ?? MapTileSource.Street;

				if (ReferenceEquals(_tileSource, value))
					return;

				_tileSource = value;
				_zoom = Math.Min(_zoom, _tileSource.MaxZoom);

				QueueDraw();
			}
		}

		/// <summary>
		/// A position to mark as "the user". The GTK backend has no location service of its own -
		/// the renderer fills this in from <see cref="FormsMaps.UserPositionProvider"/>.
		/// </summary>
		public Position? UserPosition
		{
			get => _userPosition;
			set
			{
				_userPosition = value;
				QueueDraw();
			}
		}

		public IList<Pin> Pins => _pins;

		public IList<MapElement> Elements => _elements;

		public MapSpan VisibleRegion
		{
			get
			{
				var width = Math.Max(1, AllocatedWidth);
				var height = Math.Max(1, AllocatedHeight);

				var originY = MercatorProjection.LatitudeToY(_centerLatitude, _zoom) - height / 2.0;

				var north = MercatorProjection.YToLatitude(originY, _zoom);
				var south = MercatorProjection.YToLatitude(originY + height, _zoom);

				var latitudeDegrees = Math.Abs(north - south);
				var longitudeDegrees = Math.Min(360.0, width / MercatorProjection.WorldSize(_zoom) * 360.0);

				return new MapSpan(
					new Position(_centerLatitude, _centerLongitude),
					latitudeDegrees,
					longitudeDegrees);
			}
		}

		public void MoveTo(Position center, int zoom)
		{
			_centerLatitude = MercatorProjection.ClampLatitude(center.Latitude);
			_centerLongitude = MercatorProjection.NormalizeLongitude(center.Longitude);
			_zoom = ClampZoom(zoom);

			ScheduleViewportUpdate();
		}

		/// <summary>
		/// Centres on <paramref name="span"/> and picks the largest zoom level that still contains
		/// it. Deferred until the first allocation if the widget has not been sized yet - the zoom
		/// depends on the viewport size.
		/// </summary>
		public void MoveToRegion(MapSpan span)
		{
			if (span == null)
				return;

			if (AllocatedWidth <= 1 || AllocatedHeight <= 1)
			{
				_pendingRegion = span;
				return;
			}

			ApplyRegion(span);
		}

		public void Invalidate()
		{
			QueueDraw();
		}

		void ApplyRegion(MapSpan span)
		{
			_centerLatitude = MercatorProjection.ClampLatitude(span.Center.Latitude);
			_centerLongitude = MercatorProjection.NormalizeLongitude(span.Center.Longitude);

			_zoom = ClampZoom(MercatorProjection.ZoomForSpan(
				_centerLatitude,
				span.LatitudeDegrees,
				span.LongitudeDegrees,
				AllocatedWidth,
				AllocatedHeight,
				_tileSource.MaxZoom));

			ScheduleViewportUpdate();
		}

		int ClampZoom(int zoom)
		{
			return Math.Max(MercatorProjection.MinZoomLevel, Math.Min(zoom, _tileSource.MaxZoom));
		}

		/// <summary>
		/// Coalesces "the viewport moved" into a single idle callback. Nothing here may run inline
		/// from a size-allocate: GTK 3 discards geometry changes made during allocation, and the
		/// <see cref="ViewportChanged"/> subscriber pushes a new region into the Forms element.
		/// </summary>
		void ScheduleViewportUpdate()
		{
			QueueDraw();

			if (_viewportUpdateQueued)
				return;

			_viewportUpdateQueued = true;

			GLib.Idle.Add(() =>
			{
				_viewportUpdateQueued = false;

				if (_destroyed)
					return false;

				ViewportChanged?.Invoke(this, EventArgs.Empty);

				return false;
			});
		}

		protected override void OnDestroyed()
		{
			_destroyed = true;

			base.OnDestroyed();
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);

			if (_pendingRegion != null && allocation.Width > 1 && allocation.Height > 1)
			{
				var span = _pendingRegion;
				_pendingRegion = null;

				// Deferred: applying the region changes zoom and fires ViewportChanged, and neither
				// belongs inside a size-allocate.
				GLib.Idle.Add(() =>
				{
					if (!_destroyed)
						ApplyRegion(span);

					return false;
				});

				return;
			}

			ScheduleViewportUpdate();
		}

		#region Drawing

		protected override bool OnDrawn(Cairo.Context cr)
		{
			var width = AllocatedWidth;
			var height = AllocatedHeight;

			if (width <= 0 || height <= 0)
				return true;

			// Water-ish background, so an un-fetched tile grid still reads as a map.
			cr.SetSourceRGB(0.667, 0.788, 0.859);
			cr.Rectangle(0, 0, width, height);
			cr.Fill();

			var originX = Math.Round(MercatorProjection.LongitudeToX(_centerLongitude, _zoom) - width / 2.0);
			var originY = Math.Round(MercatorProjection.LatitudeToY(_centerLatitude, _zoom) - height / 2.0);

			DrawTiles(cr, width, height, originX, originY);
			DrawElements(cr, originX, originY);
			DrawPins(cr, width, height, originX, originY);
			DrawAttribution(cr, width, height);

			return true;
		}

		void DrawTiles(Cairo.Context cr, int width, int height, double originX, double originY)
		{
			var tileCount = MercatorProjection.TileCount(_zoom);

			var firstX = (int)Math.Floor(originX / TileSize);
			var lastX = (int)Math.Floor((originX + width - 1) / TileSize);
			var firstY = Math.Max(0, (int)Math.Floor(originY / TileSize));
			var lastY = Math.Min(tileCount - 1, (int)Math.Floor((originY + height - 1) / TileSize));

			for (var tileY = firstY; tileY <= lastY; tileY++)
			{
				for (var tileX = firstX; tileX <= lastX; tileX++)
				{
					var wrappedX = ((tileX % tileCount) + tileCount) % tileCount;

					var x = tileX * TileSize - originX;
					var y = tileY * TileSize - originY;

					DrawTile(cr, _tileSource.UrlTemplate, wrappedX, tileY, x, y, true);

					if (!string.IsNullOrEmpty(_tileSource.OverlayUrlTemplate))
						DrawTile(cr, _tileSource.OverlayUrlTemplate, wrappedX, tileY, x, y, false);
				}
			}
		}

		void DrawTile(Cairo.Context cr, string template, int tileX, int tileY, double x, double y, bool drawPlaceholder)
		{
			var url = MapTileSource.FormatUrl(template, _zoom, tileX, tileY);

			if (_tiles.TryGetTile(url, out var pixbuf))
			{
				Gdk.CairoHelper.SetSourcePixbuf(cr, pixbuf, x, y);
				cr.Rectangle(x, y, TileSize, TileSize);
				cr.Fill();

				return;
			}

			_tiles.RequestTile(url, QueueDraw);

			if (!drawPlaceholder)
				return;

			// A land-coloured placeholder with a hairline grid: the map stays legible - and pins
			// and overlays stay meaningful - when tiles are slow or the machine is offline.
			cr.SetSourceRGB(0.918, 0.906, 0.878);
			cr.Rectangle(x, y, TileSize, TileSize);
			cr.Fill();

			cr.SetSourceRGB(0.847, 0.831, 0.792);
			cr.LineWidth = 1.0;
			cr.Rectangle(x + 0.5, y + 0.5, TileSize - 1, TileSize - 1);
			cr.Stroke();
		}

		void DrawElements(Cairo.Context cr, double originX, double originY)
		{
			foreach (var element in _elements)
			{
				switch (element)
				{
					case Polygon polygon:
						DrawPath(cr, polygon.Geopath, originX, originY, true, polygon.FillColor, polygon.StrokeColor, polygon.StrokeWidth);
						break;
					case Polyline polyline:
						DrawPath(cr, polyline.Geopath, originX, originY, false, Color.Default, polyline.StrokeColor, polyline.StrokeWidth);
						break;
					case Circle circle:
						DrawCircle(cr, circle, originX, originY);
						break;
				}
			}
		}

		void DrawPath(
			Cairo.Context cr,
			IList<Position> geopath,
			double originX,
			double originY,
			bool close,
			Color fillColor,
			Color strokeColor,
			float strokeWidth)
		{
			if (geopath == null || geopath.Count < 2)
				return;

			cr.NewPath();

			for (var i = 0; i < geopath.Count; i++)
			{
				var x = MercatorProjection.LongitudeToX(geopath[i].Longitude, _zoom) - originX;
				var y = MercatorProjection.LatitudeToY(geopath[i].Latitude, _zoom) - originY;

				if (i == 0)
					cr.MoveTo(x, y);
				else
					cr.LineTo(x, y);
			}

			if (close)
			{
				cr.ClosePath();

				if (!fillColor.IsDefault)
				{
					SetSource(cr, fillColor);
					cr.FillPreserve();
				}
			}

			SetStrokeSource(cr, strokeColor);
			cr.LineWidth = strokeWidth > 0 ? strokeWidth : DefaultStrokeWidth;
			cr.LineJoin = Cairo.LineJoin.Round;
			cr.LineCap = Cairo.LineCap.Round;
			cr.Stroke();
		}

		void DrawCircle(Cairo.Context cr, Circle circle, double originX, double originY)
		{
			var metersPerPixel = MercatorProjection.MetersPerPixel(circle.Center.Latitude, _zoom);

			if (metersPerPixel <= 0)
				return;

			var radius = circle.Radius.Meters / metersPerPixel;

			if (radius <= 0)
				return;

			var x = MercatorProjection.LongitudeToX(circle.Center.Longitude, _zoom) - originX;
			var y = MercatorProjection.LatitudeToY(circle.Center.Latitude, _zoom) - originY;

			cr.NewPath();
			cr.Arc(x, y, radius, 0, 2 * Math.PI);

			if (!circle.FillColor.IsDefault)
			{
				SetSource(cr, circle.FillColor);
				cr.FillPreserve();
			}

			SetStrokeSource(cr, circle.StrokeColor);
			cr.LineWidth = circle.StrokeWidth > 0 ? circle.StrokeWidth : DefaultStrokeWidth;
			cr.Stroke();
		}

		void DrawPins(Cairo.Context cr, int width, int height, double originX, double originY)
		{
			if (_userPosition.HasValue)
			{
				var ux = MercatorProjection.LongitudeToX(_userPosition.Value.Longitude, _zoom) - originX;
				var uy = MercatorProjection.LatitudeToY(_userPosition.Value.Latitude, _zoom) - originY;

				DrawUserPosition(cr, ux, uy);
			}

			foreach (var pin in _pins)
			{
				var x = MercatorProjection.LongitudeToX(pin.Position.Longitude, _zoom) - originX;
				var y = MercatorProjection.LatitudeToY(pin.Position.Latitude, _zoom) - originY;

				if (x < -TileSize || y < -TileSize || x > width + TileSize || y > height + TileSize)
					continue;

				DrawPin(cr, pin, x, y);
			}
		}

		void DrawUserPosition(Cairo.Context cr, double x, double y)
		{
			cr.NewPath();
			cr.Arc(x, y, 12, 0, 2 * Math.PI);
			cr.SetSourceRGBA(0.15, 0.45, 0.90, 0.25);
			cr.Fill();

			cr.NewPath();
			cr.Arc(x, y, 6, 0, 2 * Math.PI);
			cr.SetSourceRGB(0.10, 0.40, 0.85);
			cr.FillPreserve();
			cr.SetSourceRGB(1, 1, 1);
			cr.LineWidth = 2;
			cr.Stroke();
		}

		void DrawPin(Cairo.Context cr, Pin pin, double x, double y)
		{
			var color = ColorForPinType(pin.Type);

			cr.NewPath();
			cr.Arc(x, y - PinHeadOffset, PinHeadRadius, 0, 2 * Math.PI);
			cr.ClosePath();

			cr.MoveTo(x - PinHeadRadius * 0.72, y - PinHeadOffset + PinHeadRadius * 0.70);
			cr.LineTo(x, y);
			cr.LineTo(x + PinHeadRadius * 0.72, y - PinHeadOffset + PinHeadRadius * 0.70);
			cr.ClosePath();

			cr.SetSourceRGB(color.Item1, color.Item2, color.Item3);
			cr.FillPreserve();

			cr.SetSourceRGB(1, 1, 1);
			cr.LineWidth = 1.5;
			cr.Stroke();

			cr.NewPath();
			cr.Arc(x, y - PinHeadOffset, PinHeadRadius * 0.38, 0, 2 * Math.PI);
			cr.SetSourceRGB(1, 1, 1);
			cr.Fill();

			if (!string.IsNullOrEmpty(pin.Label))
				DrawPinLabel(cr, pin.Label, x, y - PinHeadOffset - PinHeadRadius - 4);
		}

		void DrawPinLabel(Cairo.Context cr, string text, double x, double bottom)
		{
			using (var layout = CreatePangoLayout(text))
			{
				layout.FontDescription = Pango.FontDescription.FromString(LabelFont);
				layout.GetPixelSize(out var textWidth, out var textHeight);

				const double PaddingX = 5.0;
				const double PaddingY = 2.0;

				var boxWidth = textWidth + 2 * PaddingX;
				var boxHeight = textHeight + 2 * PaddingY;
				var boxX = x - boxWidth / 2.0;
				var boxY = bottom - boxHeight;

				RoundedRectangle(cr, boxX, boxY, boxWidth, boxHeight, 3.0);
				cr.SetSourceRGBA(1, 1, 1, 0.92);
				cr.FillPreserve();
				cr.SetSourceRGBA(0.25, 0.25, 0.25, 0.55);
				cr.LineWidth = 1.0;
				cr.Stroke();

				cr.SetSourceRGB(0.13, 0.13, 0.13);
				cr.MoveTo(boxX + PaddingX, boxY + PaddingY);
				Pango.CairoHelper.ShowLayout(cr, layout);
			}
		}

		void DrawAttribution(Cairo.Context cr, int width, int height)
		{
			var attribution = _tileSource.Attribution;

			if (string.IsNullOrEmpty(attribution))
				return;

			using (var layout = CreatePangoLayout(attribution))
			{
				layout.FontDescription = Pango.FontDescription.FromString(AttributionFont);
				layout.GetPixelSize(out var textWidth, out var textHeight);

				const double PaddingX = 4.0;
				const double PaddingY = 1.0;

				var boxWidth = textWidth + 2 * PaddingX;
				var boxHeight = textHeight + 2 * PaddingY;
				var boxX = width - boxWidth;
				var boxY = height - boxHeight;

				cr.SetSourceRGBA(1, 1, 1, 0.75);
				cr.Rectangle(boxX, boxY, boxWidth, boxHeight);
				cr.Fill();

				cr.SetSourceRGB(0.2, 0.2, 0.2);
				cr.MoveTo(boxX + PaddingX, boxY + PaddingY);
				Pango.CairoHelper.ShowLayout(cr, layout);
			}
		}

		static void RoundedRectangle(Cairo.Context cr, double x, double y, double width, double height, double radius)
		{
			cr.NewPath();
			cr.Arc(x + radius, y + radius, radius, Math.PI, 1.5 * Math.PI);
			cr.Arc(x + width - radius, y + radius, radius, 1.5 * Math.PI, 2 * Math.PI);
			cr.Arc(x + width - radius, y + height - radius, radius, 0, 0.5 * Math.PI);
			cr.Arc(x + radius, y + height - radius, radius, 0.5 * Math.PI, Math.PI);
			cr.ClosePath();
		}

		static Tuple<double, double, double> ColorForPinType(PinType type)
		{
			switch (type)
			{
				case PinType.Place:
					return Tuple.Create(0.13, 0.42, 0.80);
				case PinType.SavedPin:
					return Tuple.Create(0.13, 0.60, 0.29);
				case PinType.SearchResult:
					return Tuple.Create(0.95, 0.55, 0.09);
				default:
					return Tuple.Create(0.83, 0.18, 0.18);
			}
		}

		static void SetSource(Cairo.Context cr, Color color)
		{
			cr.SetSourceRGBA(color.R, color.G, color.B, color.A);
		}

		/// <summary>
		/// Color.Default means "let the GTK theme decide", so fall back to the style context's
		/// foreground colour rather than to a hardcoded one.
		/// </summary>
		void SetStrokeSource(Cairo.Context cr, Color color)
		{
			if (color.IsDefault)
			{
				var themeColor = StyleContext.GetColor(Gtk.StateFlags.Normal);
				cr.SetSourceRGBA(themeColor.Red, themeColor.Green, themeColor.Blue, themeColor.Alpha);

				return;
			}

			SetSource(cr, color);
		}

		#endregion

		#region Input

		public Position PositionFromPixel(double px, double py)
		{
			var width = Math.Max(1, AllocatedWidth);
			var height = Math.Max(1, AllocatedHeight);

			var originX = Math.Round(MercatorProjection.LongitudeToX(_centerLongitude, _zoom) - width / 2.0);
			var originY = Math.Round(MercatorProjection.LatitudeToY(_centerLatitude, _zoom) - height / 2.0);

			return new Position(
				MercatorProjection.YToLatitude(originY + py, _zoom),
				MercatorProjection.NormalizeLongitude(MercatorProjection.XToLongitude(originX + px, _zoom)));
		}

		public Pin PinAt(double px, double py)
		{
			var width = Math.Max(1, AllocatedWidth);
			var height = Math.Max(1, AllocatedHeight);

			var originX = Math.Round(MercatorProjection.LongitudeToX(_centerLongitude, _zoom) - width / 2.0);
			var originY = Math.Round(MercatorProjection.LatitudeToY(_centerLatitude, _zoom) - height / 2.0);

			// Last drawn is topmost.
			foreach (var pin in Enumerable.Reverse(_pins))
			{
				var x = MercatorProjection.LongitudeToX(pin.Position.Longitude, _zoom) - originX;
				var y = MercatorProjection.LatitudeToY(pin.Position.Latitude, _zoom) - originY;

				var left = x - PinHeadRadius - PinHitPadding;
				var right = x + PinHeadRadius + PinHitPadding;
				var top = y - PinHeadOffset - PinHeadRadius - PinHitPadding;
				var bottom = y + PinHitPadding;

				if (px >= left && px <= right && py >= top && py <= bottom)
					return pin;
			}

			return null;
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			if (evnt.Type == Gdk.EventType.ButtonPress && evnt.Button == 1)
			{
				_dragging = true;
				_dragMoved = false;
				_dragLastX = evnt.X;
				_dragLastY = evnt.Y;

				if (CanFocus && !HasFocus)
					GrabFocus();
			}
			else if (evnt.Type == Gdk.EventType.TwoButtonPress && evnt.Button == 1 && _zoomEnabled)
			{
				ZoomAt(1, evnt.X, evnt.Y);
			}

			return true;
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			if (!_dragging)
				return base.OnMotionNotifyEvent(evnt);

			var deltaX = evnt.X - _dragLastX;
			var deltaY = evnt.Y - _dragLastY;

			if (Math.Abs(deltaX) > 2 || Math.Abs(deltaY) > 2)
				_dragMoved = true;

			_dragLastX = evnt.X;
			_dragLastY = evnt.Y;

			if (_panEnabled)
				PanByPixels(-deltaX, -deltaY);

			return true;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			if (evnt.Button != 1)
				return base.OnButtonReleaseEvent(evnt);

			var wasDrag = _dragMoved;

			_dragging = false;
			_dragMoved = false;

			if (wasDrag)
				return true;

			var pin = PinAt(evnt.X, evnt.Y);

			if (pin != null)
				PinClicked?.Invoke(this, new MapPinEventArgs(pin));
			else
				MapClicked?.Invoke(this, new MapPositionEventArgs(PositionFromPixel(evnt.X, evnt.Y)));

			return true;
		}

		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			if (!_zoomEnabled)
				return base.OnScrollEvent(evnt);

			var steps = 0;

			switch (evnt.Direction)
			{
				case Gdk.ScrollDirection.Up:
					steps = 1;
					break;
				case Gdk.ScrollDirection.Down:
					steps = -1;
					break;
				case Gdk.ScrollDirection.Smooth:
					if (evnt.DeltaY < -0.01)
						steps = 1;
					else if (evnt.DeltaY > 0.01)
						steps = -1;
					break;
			}

			if (steps == 0)
				return true;

			ZoomAt(steps, evnt.X, evnt.Y);

			return true;
		}

		/// <summary>Zooms while keeping the geographic point under the cursor fixed.</summary>
		public void ZoomAt(int steps, double px, double py)
		{
			var target = ClampZoom(_zoom + steps);

			if (target == _zoom)
				return;

			var anchor = PositionFromPixel(px, py);

			var width = Math.Max(1, AllocatedWidth);
			var height = Math.Max(1, AllocatedHeight);

			_zoom = target;

			// Recentre so `anchor` lands back under (px, py) at the new zoom.
			var anchorX = MercatorProjection.LongitudeToX(anchor.Longitude, _zoom);
			var anchorY = MercatorProjection.LatitudeToY(anchor.Latitude, _zoom);

			var centerX = anchorX - px + width / 2.0;
			var centerY = anchorY - py + height / 2.0;

			_centerLongitude = MercatorProjection.NormalizeLongitude(MercatorProjection.XToLongitude(centerX, _zoom));
			_centerLatitude = MercatorProjection.ClampLatitude(MercatorProjection.YToLatitude(centerY, _zoom));

			ScheduleViewportUpdate();
		}

		public void PanByPixels(double deltaX, double deltaY)
		{
			var centerX = MercatorProjection.LongitudeToX(_centerLongitude, _zoom) + deltaX;
			var centerY = MercatorProjection.LatitudeToY(_centerLatitude, _zoom) + deltaY;

			var worldSize = MercatorProjection.WorldSize(_zoom);

			centerX = ((centerX % worldSize) + worldSize) % worldSize;
			centerY = Math.Max(0, Math.Min(worldSize, centerY));

			_centerLongitude = MercatorProjection.NormalizeLongitude(MercatorProjection.XToLongitude(centerX, _zoom));
			_centerLatitude = MercatorProjection.ClampLatitude(MercatorProjection.YToLatitude(centerY, _zoom));

			ScheduleViewportUpdate();
		}

		#endregion
	}
}
