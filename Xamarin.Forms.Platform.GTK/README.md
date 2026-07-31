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

## Threading

GTK is not thread-safe. Everything that touches a widget must run on the thread that called
`Forms.Init()` — the thread running the GTK main loop. Use `Device.BeginInvokeOnMainThread` from
anywhere else.

## Optional features

Two areas are quarantined behind MSBuild switches and compiled out by default, because each
needs native dependencies that are not part of a base GTK3 install. Set the property in the
consuming project and rebuild this backend from source to enable them:

| Property | Enables | Needs |
| --- | --- | --- |
| `EnableGtkWebView` | `WebView` via WebKit2GTK | `libwebkit2gtk-4.1` |
| `EnableGtkOpenGL` | `OpenGLView` via `Gtk.GLArea` | An OpenGL-capable GTK build |

## Dependencies

`Net4x.Xamarin.Forms.Core` and `GtkSharp` 3.24.24.95.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/xamarin/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); note that
.NET MAUI has no GTK backend, which is why this one is still maintained here.
