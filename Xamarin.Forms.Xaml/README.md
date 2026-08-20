# Net4x.Xamarin.Forms.Xaml

The runtime XAML layer for Xamarin.Forms: it turns the XAML embedded in your assembly into a
live object graph.

```
dotnet add package Net4x.Xamarin.Forms.Xaml
```

## What is in it

`Xamarin.Forms.Xaml.dll` (`netstandard2.0`) provides:

- **`LoadFromXaml`** — the loader `InitializeComponent()` calls, and the `XamlLoader` /
  `XamlParser` behind it.
- **Markup extensions** — `{Binding}`, `{StaticResource}`, `{DynamicResource}`, `{x:Static}`,
  `{x:Reference}`, `{x:Type}`, `{OnPlatform}`, `{OnIdiom}`, `{AppThemeBinding}` and the rest.
- **Type converters and value providers** used when a XAML attribute is assigned to a
  strongly-typed property.
- **`[XamlCompilation]`**, which selects between runtime parsing and IL compiled at build time.

## The other half

XAML is two layers. This package is the runtime one; the build-time one lives in
[`Net4x.Xamarin.Forms.Build.Tasks`](https://www.nuget.org/packages/Net4x.Xamarin.Forms.Build.Tasks),
which generates the `InitializeComponent` + `x:Name` partial class (`XamlGTask`) and can compile
XAML straight to IL (`XamlCTask`). Reference both if your project contains `.xaml` files.

## Dependencies

`Net4x.Xamarin.Forms.Core`.

## About this fork

Maintenance fork of the `5.0.0` branch of [xamarin/Xamarin.Forms](https://github.com/pieroviano/Xamarin.Forms),
published under the `Net4x.` package prefix. Upstream Xamarin.Forms reached end of support on
**May 1, 2024** and was succeeded by [.NET MAUI](https://github.com/dotnet/maui); new
applications should target .NET MAUI instead.
