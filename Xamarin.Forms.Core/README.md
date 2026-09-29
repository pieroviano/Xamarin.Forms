# Net4x.Xamarin.Forms.Core

The platform-independent half of Xamarin.Forms — the shared UI abstraction that per-platform
renderers map onto native controls at runtime.

```
dotnet add package Net4x.Xamarin.Forms.Core
```

You normally do not reference this package directly: a backend package such as
[`Net4x.Xamarin.Forms.Platform.GTK`](https://www.nuget.org/packages/Net4x.Xamarin.Forms.Platform.GTK)
brings it in.

## What is in it

`Xamarin.Forms.Core.dll` (`netstandard2.0`) contains everything that does not depend on a
specific platform:

- **The property system** — `BindableObject` / `BindableProperty`, backing data bindings,
  styles and triggers.
- **The visual tree** — `Element` → `VisualElement` → `View` / `Page` / `Layout`, plus layout
  and measurement.
- **`Shell`** — flyout/tab navigation and URI-based routing.
- **Resources and styling** — `ResourceDictionary`, `Style`, and CSS `StyleSheets`.
- **App services** — `MessagingCenter`, `DependencyService`, `Device`, the animation system.
- **`PlatformConfiguration`** — the `<Platform>Specific` extension-method surface.
- **`Xamarin.Forms.Internals.Registrar`**, which maps a Forms type to a renderer type (keyed by
  `Visual` and priority) and is populated from `[assembly: ExportRenderer]` and friends during
  each backend's `Forms.Init(...)`.

## Companion packages

| Package | Purpose |
| --- | --- |
| `Net4x.Xamarin.Forms.Platform` | Facade this package references; the real assembly is supplied per platform |
| `Net4x.Xamarin.Forms.Xaml` | Runtime XAML loader and markup extensions |
| `Net4x.Xamarin.Forms.Build.Tasks` | Build-time XAML code generation and IL compilation |
| `Net4x.Xamarin.Forms.Maps` | `Map` control abstraction |
| `Net4x.Xamarin.Forms.Platform.GTK` | GTK3 backend |

`AssemblyVersion` is deliberately frozen at `2.0.0.0` for binding compatibility; the package
version is the one that moves.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/pieroviano/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); new
applications should target .NET MAUI instead.
