# Net4x.Xamarin.Forms.Platform.GTK

The GTK3 backend for Xamarin.Forms — renderers that map the Forms visual tree onto native GTK
widgets, built on [GtkSharp 3](https://www.nuget.org/packages/GtkSharp).

```
dotnet add package Net4x.Xamarin.Forms.Platform.GTK
```

This is the package a GTK desktop app references; it pulls in `Net4x.Xamarin.Forms.Core`.

## Getting started

```csharp
using Gtk;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;

[STAThread]
static void Main(string[] args)
{
    GtkThemes.Init();
    Application.Init();
    Forms.Init();

    var window = new FormsWindow();
    window.LoadApplication(new App());
    window.SetApplicationTitle("My App");
    window.SetApplicationIcon("icon.png");
    window.Show();

    Application.Run();
}
```

`Forms.Init()` populates the renderer registry by scanning for `[assembly: ExportRenderer]`,
`ExportCell`, `ExportImageSourceHandler`, `ExportFont` and `ExportEffect` attributes; pass extra
assemblies to `Forms.Init(IEnumerable<Assembly>)` when your renderers live outside the app
assembly.

## Native prerequisites

GTK 3 must be installed on the machine running the app — `libgtk-3-0` on Linux, the
[GTK3 runtime](https://github.com/GtkSharp/GtkSharp) on Windows, `gtk+3` via Homebrew on macOS.
GtkSharp binds these libraries at runtime; a missing GTK3 shows up as a `DllNotFoundException`
on the first widget touch.

`WebView` additionally needs **WebKit2GTK 4.1** — `libwebkit2gtk-4.1-0` on Debian/Ubuntu,
`webkit2gtk4.1` on Fedora. This one is optional: the backend loads it lazily and, when it is
absent, a `WebView` renders a "WebView unavailable" placeholder instead of throwing, so the rest
of the application is unaffected.

## Threading

GTK is not thread-safe. Everything that touches a widget must run on the thread that called
`Forms.Init()` — the thread running the GTK main loop. Use `Device.BeginInvokeOnMainThread` from
anywhere else.

## Optional features

**Nothing is behind an MSBuild switch any more.** Every renderer is always compiled in; the two
that depend on something outside a base GTK3 install degrade at runtime instead of at build time,
so there is no property to set and no need to rebuild this backend from source:

| Renderer | Needs | Without it |
| --- | --- | --- |
| `OpenGLView` (`Gtk.GLArea`) | An OpenGL-capable GTK build — GTK 3.16+ plus a usable GL/EGL driver | GTK stores a `GError` on the area and paints its own GL error placeholder inside the view's bounds. Nothing throws, and the application stays up. |
| `WebView` (WebKit2GTK) | `libwebkit2gtk-4.1.so.0` | The renderer shows a placeholder (see *Native prerequisites*). |

The former `EnableGtkOpenGL` / `EnableGtkWebView` properties are gone, along with the OpenTK
dependency `OpenGLView` used to carry: it now runs on `Gtk.GLArea`, which is part of GTK itself.

## Maps

`Xamarin.Forms.Maps` **is** supported on GTK, in a separate package:

```
dotnet add package Net4x.Xamarin.Forms.Maps.GTK
```

It draws its own Web-Mercator slippy map with Cairo over OpenStreetMap-compatible raster tiles, so
it needs **no API key and no native dependency beyond GTK 3 itself**. `MapType`, `MoveToRegion`,
`VisibleRegion`, pan/zoom, `Pins` and `MapElements` (`Polyline`/`Polygon`/`Circle`) are implemented,
and `Geocoder` is backed by Nominatim. Call `Xamarin.Forms.Maps.GTK.FormsMaps.Init()` after
`Forms.Init()`. See that package's README for its known limitations (notably `IsShowingUser`, which
needs an application-supplied position provider because GTK has no location service) and for how to
point it at your own tile server before shipping.

## Dependencies

`Net4x.Xamarin.Forms.Core` and `GtkSharp` 3.24.24.95.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/pieroviano/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); note that
.NET MAUI has no GTK backend, which is why this one is still maintained here.
