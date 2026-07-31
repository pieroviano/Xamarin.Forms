# Net4x.Xamarin.Forms.Maps.GTK

`Xamarin.Forms.Maps` support for the GTK3 backend.

```
dotnet add package Net4x.Xamarin.Forms.Maps.GTK
```

## How it works

There is no GTK 3 map widget to bind:

* `GMap.NET.GTK` — what this project used before — is GTK 2 and renders through `System.Drawing`,
  and has no GTK 3 successor.
* `libosmgpsmap-1.0` exists, but only in Ubuntu `universe`/Debian `contrib`; it is not installed on
  a stock GTK desktop, and it has no polygon or circle primitive.

So this package draws the map itself: a Web-Mercator "slippy map" of XYZ raster tiles, rendered
with Cairo onto a `Gtk.DrawingArea` (`Xamarin.Forms.Maps.GTK.Controls.MapControl`). The only
dependency is GTK 3 itself.

## Getting started

```csharp
GtkThemes.Init();
Application.Init();
Forms.Init();
Xamarin.Forms.Maps.GTK.FormsMaps.Init();   // registers the Nominatim Geocoder backend
```

`FormsMaps.Init(string authenticationToken)` is accepted for source compatibility with the other
backends; the default tile layers need no key.

If your renderers are not in the application assembly, pass the assembly to `Forms.Init` so the
registrar finds `[assembly: ExportRenderer(typeof(Map), typeof(MapRenderer))]`:

```csharp
Forms.Init(new[] { typeof(Xamarin.Forms.Maps.GTK.MapRenderer).Assembly });
```

## Supported

| Feature | Notes |
|---|---|
| `Map.MapType` | `Street`, `Satellite`, `Hybrid` (`Hybrid` = imagery plus a transparent labels layer) |
| `Map.MoveToRegion` / `MapSpan` | centres, then picks the largest zoom level that still contains the span |
| `Map.VisibleRegion` | recomputed from the allocation on every pan, zoom and resize |
| `Map.HasScrollEnabled` / `HasZoomEnabled` | drag to pan, wheel or double-click to zoom (zoom keeps the point under the cursor fixed) |
| `Map.Pins` | teardrop markers, coloured by `Pin.Type`, with the `Pin.Label` in a callout |
| `Pin.MarkerClicked` / `Pin.Clicked` | raised on a click that hits a marker |
| `Map.MapClicked` | raised on a click that misses every marker and was not a drag |
| `Map.MapElements` | `Polyline`, `Polygon` (with `FillColor`) and `Circle` (`Distance`-accurate radius) |
| `Map.MoveToLastRegionOnLayoutChange` | re-fits `LastMoveToRegion` when the map is resized |
| `Geocoder` | `GetPositionsForAddressAsync` / `GetAddressesForPositionAsync` via Nominatim |

## Known limitations

| Limitation | Detail |
|---|---|
| `Map.IsShowingUser` needs a position provider | GTK has no location service. Set `FormsMaps.UserPositionProvider` (a `Func<Task<Position?>>`) and the map draws the usual blue dot, polling every `FormsMaps.UserPositionRefreshInterval`. Without one the flag does nothing. |
| `Map.TrafficEnabled` is ignored | No public tile layer provides live traffic. |
| Raster tiles only | No vector tiles, no 3D, no rotation/tilt. Zoom is by whole levels (no continuous pinch zoom). |
| `Pin.InfoWindowClicked` is never raised | The label callout is drawn, not a separate hit-tested window; clicking a pin raises `MarkerClicked` only. |
| `Map.ItemsSource` templating draws as plain pins | `Map` materialises `ItemTemplate` into `Pin`s in Core; the GTK renderer draws those pins, it does not render arbitrary `View`s on the map. |

## Tile servers

The defaults point at public community servers:

| `MapType` | Layer | Attribution drawn on the map |
|---|---|---|
| `Street` | `https://tile.openstreetmap.org/{z}/{x}/{y}.png` | `© OpenStreetMap contributors` |
| `Satellite` / `Hybrid` | Esri *World Imagery* | `Imagery © Esri` |

**Their usage policies forbid heavy or commercial traffic.** Point the map at your own tile server
before shipping — either per layer:

```csharp
MapTileSource.Street = new MapTileSource(
    "https://tiles.example.com/{z}/{x}/{y}.png", "© Example");
```

or for everything at once:

```csharp
FormsMaps.TileSourceForMapType = type => myTileSource;
```

Tiles are cached in memory and, between runs, under
`$XDG_DATA_HOME/Xamarin.Forms.Maps.GTK/tiles`; set `TileCache.CacheDirectory = null` to disable the
disk cache. `Geocoder` queries go to `https://nominatim.openstreetmap.org` — the same caveat
applies, and `GeocoderBackend.ServiceRoot` redirects it.

When a tile cannot be fetched (offline, throttled) the map draws a land-coloured grid in its place,
so pins, polylines and polygons stay visible and correctly positioned.
