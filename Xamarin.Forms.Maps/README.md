# Net4x.Xamarin.Forms.Maps

The platform-independent `Map` control for Xamarin.Forms.

```
dotnet add package Net4x.Xamarin.Forms.Maps
```

## What is in it

`Xamarin.Forms.Maps.dll` (`netstandard2.0`) is the abstraction only:

- **`Map`** — the control, with `MapType`, `MapSpan`/`MoveToRegion`, and `MapClicked`.
- **Geometry** — `Position`, `Distance`, `MapSpan`, `GeographyUtils`.
- **Overlays** — `Pin` (`PinType`, `PinClicked`), `Circle`, `Polygon`, `Polyline`, `MapElement`.
- **`Geocoder`** — address ⇄ position lookup, backed by the platform's own geocoding service.

## A renderer is required

This package draws nothing on its own. It needs a matching per-platform maps renderer, whose
`FormsMaps.Init(...)` must be called after `Forms.Init()` — and, on most platforms, a maps API
key configured in the host app.

> **This fork does not currently ship a maps backend.** `Xamarin.Forms.Maps.GTK` is still on
> GTK# 2 and is not part of `Xamarin.Forms.Gtk.sln`, so `Map` has no GTK3 renderer yet. The
> package is published because `Net4x.Xamarin.Forms.Maps` remains the reference other code
> compiles against.

## Dependencies

`Net4x.Xamarin.Forms.Core`.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/xamarin/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); new
applications should target .NET MAUI instead.
